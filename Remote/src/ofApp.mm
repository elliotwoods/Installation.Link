#include "ofApp.h"

#define PROJECTOR_COUNT 8

//--------------------------------------------------------------
void ofApp::setup(){
	// Notes :
	//	* libmysql is built for armv7
	//	* On iPhone. Start the application with the device in portrait

	ofSetOrientation(OF_ORIENTATION_DEFAULT);//Set iOS to Orientation Landscape Right
	ofSetFrameRate(60);
	ofSetFullscreen(true);

	this->timeOfLastAction = 0.0f;
	
	this->gui.init();
    
    auto portrait = ofGetWidth() < ofGetHeight();
    auto width = ofGetWidth();
    auto height = ofGetHeight();
    
    //let's setup the zoom based on the device width
    {
        
        auto zoom = 1.0f;
        switch(width) {
            case 1536:
                zoom = 0.75f;
                break;
            case 640:
                zoom = 0.5f;
                break;
            default:
                break;
        }
        
        if(zoom != 1.0f) {
            this->gui.setZoom(zoom);
        }
    }
    
    width = this->gui.getWidth();
    height = this->gui.getHeight();
	
	this->gui.setBrokenRotation(false); //set to true for iPad 2 and iPad Air 2 (iOS 8.2 issue?
	
	this->connection = shared_ptr<Connection>(new Connection());
	this->connection->setBounds(ofRectangle(20, 40, 480, 100));
	
	this->canvas = shared_ptr<Canvas>(new Canvas());
	this->canvas->setConnection(this->connection);
	this->canvas->setBounds(ofRectangle(0, 0, this->gui.getWidth(), this->gui.getHeight()));
	this->gui.add(this->canvas);
	this->gui.add(this->connection);
	
	ofAddListener(this->connection->onHitSimple, this->canvas.get(), &Canvas::refresh);
	ofAddListener(this->canvas->onHitSimple, this, &ofApp::noteAction);
	
	this->addButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("plus")));
	this->addButton->setBounds(ofRectangle(this->gui.getWidth() - 220, this->gui.getHeight() - 220, 150, 150));
	ofAddListener(this->addButton->onButtonHit, this->canvas.get(), &Canvas::addNew);
	this->gui.add(this->addButton);
	
	
	this->clearSelectionButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("tick")));
	this->clearSelectionButton->setBounds(ofRectangle(this->canvas->getBounds().x + 70, this->gui.getHeight() - 220, 150, 150));
	ofAddListener(this->clearSelectionButton->onButtonHit, this->canvas.get(), &Canvas::clearSelection);
	this->gui.add(this->clearSelectionButton);
	
	this->deleteButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("cross")));
	this->deleteButton->setBounds(ofRectangle(this->canvas->getBounds().x + 70, this->gui.getHeight() - (220 + 200), 150, 150));
	ofAddListener(this->deleteButton->onButtonHit, this->canvas.get(), &Canvas::deleteSelection);
	this->gui.add(this->deleteButton);
	
	this->sendToBackButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("sendToBack")));
	this->sendToBackButton->setBounds(ofRectangle(this->canvas->getBounds().x + 70, this->gui.getHeight() - (220 + 400), 150, 150));
	ofAddListener(this->sendToBackButton->onButtonHit, this->canvas.get(), &Canvas::sendSelectionToBack);
	this->gui.add(this->sendToBackButton);
	
	
	this->flipHorizontalButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("flipHorizontal")));
	this->flipHorizontalButton->setBounds(ofRectangle(this->canvas->getBounds().x + (70 + 200), this->gui.getHeight() - 220, 150, 150));
	ofAddListener(this->flipHorizontalButton->onButtonHit, this->canvas.get(), &Canvas::flipHorizontal);
	this->gui.add(this->flipHorizontalButton);
	
	this->flipVerticalButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("flipVertical")));
	this->flipVerticalButton->setBounds(ofRectangle(this->canvas->getBounds().x + (70 + 200 + 200), this->gui.getHeight() - 220, 150, 150));
	ofAddListener(this->flipVerticalButton->onButtonHit, this->canvas.get(), &Canvas::flipVertical);
	this->gui.add(this->flipVerticalButton);
	
	this->rotateRightButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("rotateRight")));
	this->rotateRightButton->setBounds(ofRectangle(this->canvas->getBounds().x + (70 + 200 + 200 + 200), this->gui.getHeight() - 220, 150, 150));
	ofAddListener(this->rotateRightButton->onButtonHit, this->canvas.get(), &Canvas::rotateRight);
	this->gui.add(this->rotateRightButton);
	
	this->snapPointButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("snapPoint")));
	this->snapPointButton->setBounds(ofRectangle(this->canvas->getBounds().x + (70 + 200), this->gui.getHeight() - (220 + 200), 150, 150));
	ofAddListener(this->snapPointButton->onButtonHit, this->canvas.get(), &Canvas::snapPoint);
	this->gui.add(this->snapPointButton);
	
	this->resetQuadButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("resetQuad")));
	this->resetQuadButton->setBounds(ofRectangle(this->canvas->getBounds().x  + (70 + 200 + 200), this->gui.getHeight() - (220 + 200), 150, 150));
	ofAddListener(this->resetQuadButton->onButtonHit, this->canvas.get(), &Canvas::resetQuad);
	this->gui.add(this->resetQuadButton);
	
	
	this->projectorSelection = shared_ptr<ProjectorSelection>(new ProjectorSelection());
	this->projectorSelection->setBounds(ofRectangle(20, 140, width / 2, 100));
    for(int i=0; i<PROJECTOR_COUNT; i++) {
        this->projectorSelection->addOption(ofToString(i));
    }
	this->gui.add(this->projectorSelection);
	this->canvas->setProjectorSelection(this->projectorSelection);
	
	this->typeSelection = shared_ptr<TypeSelection>(new TypeSelection());
    if(portrait) {
        this->typeSelection->setBounds(ofRectangle(20, 140 + 100 + 20, this->gui.getWidth() - (20 + 20), 100));
    }
    else {
        this->typeSelection->setBounds(ofRectangle(20 + 460 + 20, 140, this->gui.getWidth() - (20 + 460 + 20) - 20, 100));
    }
	this->typeSelection->setConnection(this->connection);
	this->gui.add(this->typeSelection);
	this->canvas->setTypeSelection(this->typeSelection);
	
	this->viewModeSelection = shared_ptr<ViewModeSelection>(new ViewModeSelection());
    this->viewModeSelection->addOption("");
	this->viewModeSelection->addOption("Live");
	this->viewModeSelection->addOption("Edit");
	this->viewModeSelection->addOption("Grid");
	this->viewModeSelection->setBounds(ofRectangle(this->typeSelection->getBounds().x, 20, width / 2, 100));
	this->gui.add(this->viewModeSelection);
}


//--------------------------------------------------------------
void ofApp::update(){
	this->gui.update();

	bool hasSelection;
	if(this->canvas->getSelection()) {
		hasSelection = true;
	} else {
		hasSelection = false;
	}

	this->clearSelectionButton->setEnabled(hasSelection);
	this->deleteButton->setEnabled(hasSelection);
	this->sendToBackButton->setEnabled(hasSelection);
	
	this->flipHorizontalButton->setEnabled(hasSelection);
	this->flipVerticalButton->setEnabled(hasSelection);
	this->rotateRightButton->setEnabled(hasSelection);
	this->snapPointButton->setEnabled(hasSelection);
	this->resetQuadButton->setEnabled(hasSelection);
	
	if (ofGetElapsedTimef() - timeOfLastAction > 1.0f) {
		this->canvas->refresh();
		this->noteAction();
	}
}

//--------------------------------------------------------------
void ofApp::draw(){
	ofBackgroundGradient(60, 0);
	this->gui.draw();
}

//--------------------------------------------------------------
void ofApp::exit(){
    
}

//--------------------------------------------------------------
void ofApp::touchDown(ofTouchEventArgs & touch){
	this->noteAction();
}

//--------------------------------------------------------------
void ofApp::touchMoved(ofTouchEventArgs & touch){
	this->noteAction();
}

//--------------------------------------------------------------
void ofApp::touchUp(ofTouchEventArgs & touch){
	this->noteAction();
}

//--------------------------------------------------------------
void ofApp::touchDoubleTap(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchCancelled(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::lostFocus(){
    
}

//--------------------------------------------------------------
void ofApp::gotFocus(){
    
}

//--------------------------------------------------------------
void ofApp::gotMemoryWarning(){
    
}

//--------------------------------------------------------------
void ofApp::deviceOrientationChanged(int newOrientation){
    
}

//--------------------------------------------------------------
void ofApp::noteAction() {
	this->timeOfLastAction = ofGetElapsedTimef();
	this->pushOsc();
}

void ofApp::pushOsc() {
	auto osc = this->connection->getOscSender();
	if(osc) {
		ofxOscBundle bundle;
		{
			ofxOscMessage msg;
			msg.setAddress("/Cursor");
			auto selectedCorner = this->canvas->getSelectedCornerLocation();
			msg.addFloatArg(selectedCorner.x);
			msg.addFloatArg(selectedCorner.y);
			bundle.addMessage(msg);
		}
		{
			ofxOscMessage msg;
			msg.setAddress("/iProjector");
			msg.addIntArg(this->projectorSelection->getSelectionIndex());
			bundle.addMessage(msg);
		}
		{
            auto selectedIndex = this->viewModeSelection->getSelectionIndex();
            if(selectedIndex != 0) {
                selectedIndex -= 1; // it was offset by 1
                ofxOscMessage msg;
                msg.setAddress("/View/Mode");
                msg.addIntArg(selectedIndex);
                bundle.addMessage(msg);
            }
		}
		{
			ofxOscMessage msg;
			msg.setAddress("/View/Transform");
			auto transform = this->canvas->getViewTransform();
			for(int i=0; i<16; i++) {
				msg.addFloatArg(transform.getPtr()[i]);
			}
			bundle.addMessage(msg);
		}
		try {
			osc->sendBundle(bundle);
		} catch (std::exception e) {
			cout << e.what();
		}
	}
}
