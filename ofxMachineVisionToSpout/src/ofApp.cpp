#include "pch_MachineVisionToSpout.h"
#include "ofApp.h"

//--------------------------------------------------------------
void ofApp::setup(){
	ofSetVerticalSync(true);
	ofSetFrameRate(30);

	//setup the grabber
	this->grabber.setDevice(make_shared<ofxMachineVision::Device::VideoInput>());
	{
		auto initialisationSettings = static_pointer_cast<ofxMachineVision::Device::VideoInput::InitialisationSettings>(this->grabber.getDefaultInitialisationSettings());
		initialisationSettings->width = 1280;
		initialisationSettings->height = 720;
		this->grabber.open(initialisationSettings);
	}
	this->grabber.startCapture();

	//load haar classifier
	this->faceCascade.load(ofToDataPath("haarcascade_frontalface_default.xml"));

	//setup the gui
	this->gui.init();
	{
		auto panel = this->gui.add(this->grabber, "VideoInput");
		panel->onDrawImage += [this](ofxCvGui::DrawImageArguments & args) {
			ofPushStyle();
			{
				ofNoFill();
				ofSetLineWidth(2.0f);
				ofSetColor((ofGetElapsedTimeMillis() / 2) % 255);
				for (const auto & face : this->faces) {
					ofDrawRectangle(face);
				}

				ofNoFill();
				ofSetLineWidth(3.0f);
				ofSetColor(0);
				ofDrawRectangle(this->cropRectangle);
				ofSetLineWidth(1.0f);
				ofSetColor(255);
				ofDrawRectangle(this->cropRectangle);
				
			}
			ofPopStyle();

			stringstream status;
			status << this->grabber.getWidth() << "x" << this->grabber.getHeight();
			ofxCvGui::Utils::drawText(status.str(), 30, 100);
		};
		panel->onMouse += [this](ofxCvGui::MouseArguments & args) {
			this->cropRectangle.x += args.movement.x;
			this->cropRectangle.y += args.movement.y;
		};
		panel->onDraw += [this](ofxCvGui::DrawArguments & args) {
			if (this->isRecording) {
				ofPushStyle();
				{
					ofEnableAlphaBlending();
					ofSetLineWidth(10.0f);
					ofNoFill();
					ofSetColor(255, 0, 0, 127);
					ofDrawRectangle(args.localBounds);
				}
				ofPopStyle();
			}
		};
	}
	{
		auto panel = this->gui.add(this->sharpened, "Sharpened");
	}
	{
		auto panel = this->gui.add(this->cropped, "Cropped");
	}

	//setup the widgets
	{
		auto widgets = this->gui.addWidgets();
		widgets->addTitle("MachineVision to Spout", ofxCvGui::Widgets::Title::Level::H1);
		widgets->addFps();
		widgets->addMemoryUsage();
		widgets->addLiveValueHistory("Camera fps", [this]() {
			return this->grabber.getFps();
		});
		widgets->addTitle("Camera parameters", ofxCvGui::Widgets::Title::Level::H2);
		{
			auto parameters = this->grabber.getDevice()->getParameters();
			for (auto parameter : parameters) {
				auto floatParameter = parameter->getParameterTyped<float>();
				if (floatParameter) {
					auto slider = widgets->addSlider(*floatParameter);
					slider->onValueChange += [parameter](const float &) {
						parameter->syncToDevice();
					};
				}
			}
		}
		widgets->addParameterGroup(this->postProcessing);
	}

	//setup the face tracking thread
	{
		this->faceTrackingThread = thread([this]() {
			cv::Mat grayscale;

			while (this->threadRunning) {
				ofPixels pixels;
				bool pixelsRecieved = false;
				while (this->incomingCameraFrames.tryReceive(pixels)) {
					pixelsRecieved = true;
				}
				if(pixelsRecieved) {
					try {
						const auto reduceScale = 2;
						cv::cvtColor(ofxCv::toCv(pixels), grayscale, CV_RGB2GRAY);
						cv::resize(grayscale, grayscale, grayscale.size() / reduceScale);
						vector<cv::Rect> faces;
						this->faceCascade.detectMultiScale(grayscale, faces, 1.3, 5);

						vector<ofRectangle> ofFaces;
						for (const auto & face : faces) {
							auto ofFace = ofxCv::toOf(face);
							ofFace.x *= reduceScale;
							ofFace.y *= reduceScale;
							ofFace.width *= reduceScale;
							ofFace.height *= reduceScale;
							ofFaces.emplace_back(move(ofFace));
						}

						this->incomingFaces.send(ofFaces);
					}
					catch (cv::Exception e) {
						cout << e.what();
					}
				}
			}
		});
	}

	//setup the spout sender
	{
		//seems to fail sometimes so lets try hard
		for (int i = 0; i < 20; i++) {
			if (this->spoutSender.init("croppedCamera", this->grabber.getWidth() / 2, this->grabber.getHeight() / 2)) {
				break;
			}
			cout << "Failed to open spout sender #" << i << endl;
			ofSleepMillis(5000);
		}
		if (!this->spoutSender.isInitialized()) {
			ofSystemAlertDialog("Couldn't open the spout sender");
		}
	}
	//setup the osc listener
	this->oscReceiver.setup(2345);
}

//--------------------------------------------------------------
void ofApp::update(){
	this->grabber.update();

	auto fullFrame = ofRectangle(0, 0, this->grabber.getWidth(), this->grabber.getHeight());
	auto now = chrono::system_clock::now();

	//get camera frame for thread
	{
		this->incomingCameraFrames.send(this->grabber.getPixels());
	}
	
	//sharpen image
	{
		auto image = ofxCv::toCv(this->grabber.getPixels());
		if (this->postProcessing.sharpening.radius > 0.0f) {
			cv::Mat blurSharpen;
			cv::GaussianBlur(image, blurSharpen, cv::Size(0, 0), this->postProcessing.sharpening.radius);
			cv::addWeighted(image, 1.5, blurSharpen, -0.5, 0, blurSharpen);
			ofxCv::copy(blurSharpen, this->sharpened.getPixels());
		}
		else {
			ofxCv::copy(image, this->sharpened.getPixels());
		}
		this->sharpened.update();
	}

	//receive tracked faces
	{
		while (this->incomingFaces.tryReceive(this->faces)) {

		}
	}

	//listen for messages
	{
		ofxOscMessage msg;
		while (this->oscReceiver.getNextMessage(msg)) {
			this->lastMessageReceived = now;
		}
		this->isRecording = now - this->lastMessageReceived < chrono::seconds(0.5f);
	}

	//update the crop rectangle
	if(!this->isRecording) {
		if (this->cropRectangle.getWidth() == 0) {
			//initialize
			this->cropRectangle.width = fullFrame.width / 3.0f;
			this->cropRectangle.height = cropRectangle.width; //make square
			this->cropRectangle.x = this->cropRectangle.width / 2.0f;
			this->cropRectangle.y = this->cropRectangle.height / 2.0f;
			this->cropped.allocate(this->cropRectangle.width, this->cropRectangle.height, GL_RGBA);
		}

		ofVec2f targetCenter;
		if (this->faces.empty()) {
			//if we have no faces, go to center
			targetCenter = fullFrame.getCenter();
		}
		else {
			//go to the mean of the centers
			for (const auto & face : this->faces) {
				targetCenter += face.getCenter();
			}
			targetCenter /= (float) this->faces.size();
		}

		//check if any faces are completely inside crop
		bool anyInsideCompletely = false;
		for (const auto & face : this->faces) {
			if (this->cropRectangle.inside(face)) {
				anyInsideCompletely = true;
				break;
			}
		}

		//check if any faces are completely outside crop
		bool allCompletelyOutside = true;
		for (const auto & face : this->faces) {
			if (this->cropRectangle.inside(face.getCenter())) {
				allCompletelyOutside = false;
				break;
			}
		}

		//jump to contain face if it's outside
		if (!this->faces.empty() && allCompletelyOutside) {
			auto target = this->faces[0].getCenter();
			
			
			this->cropRectangle.x = target.x - this->cropRectangle.width / 2.0f;
			this->cropRectangle.y = target.y - this->cropRectangle.height / 2.0f;
		}

		//move the crop rectangle if needs updating
		if(!anyInsideCompletely) {
			auto speed = !this->faces.empty() ? 2.0f : 1.0f;
			{
				auto delta = targetCenter.x - this->cropRectangle.getCenter().x;
				if (delta == delta && delta != 0) {
					this->cropRectangle.x += delta / abs(delta) * speed;
				}
			}
			{
				auto delta = targetCenter.y - this->cropRectangle.getCenter().y;
				if (delta == delta && delta != 0) {
					this->cropRectangle.y += delta / abs(delta) * speed;
				}
			}
		}

		//check that crop rectangle fits inside the full frame
		{
			cropRectangle.x = ofClamp(cropRectangle.x, 0, fullFrame.width - this->cropRectangle.width);
			cropRectangle.y = ofClamp(cropRectangle.y, 0, fullFrame.height - this->cropRectangle.height);
		}
	}

	//perform the crop
	{
		this->cropped.begin();
		{
			ofTranslate(-this->cropRectangle.getTopLeft());
			this->sharpened.draw(0, 0);
		}
		this->cropped.end();
	}

	//send the image
	{
		if (this->cropped.isAllocated()) {
			this->spoutSender.send(this->cropped.getTexture());
		}
	}
}

//--------------------------------------------------------------
void ofApp::draw(){

}

//--------------------------------------------------------------
void ofApp::exit()
{
	this->threadRunning = false;
	this->faceTrackingThread.join();
}

//--------------------------------------------------------------
void ofApp::keyPressed(int key){

}

//--------------------------------------------------------------
void ofApp::keyReleased(int key){

}

//--------------------------------------------------------------
void ofApp::mouseMoved(int x, int y ){

}

//--------------------------------------------------------------
void ofApp::mouseDragged(int x, int y, int button){

}

//--------------------------------------------------------------
void ofApp::mousePressed(int x, int y, int button){

}

//--------------------------------------------------------------
void ofApp::mouseReleased(int x, int y, int button){

}

//--------------------------------------------------------------
void ofApp::mouseEntered(int x, int y){

}

//--------------------------------------------------------------
void ofApp::mouseExited(int x, int y){

}

//--------------------------------------------------------------
void ofApp::windowResized(int w, int h){

}

//--------------------------------------------------------------
void ofApp::gotMessage(ofMessage msg){

}

//--------------------------------------------------------------
void ofApp::dragEvent(ofDragInfo dragInfo){ 

}
