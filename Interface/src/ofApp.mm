#include "ofApp.h"

//--------------------------------------------------------------
void ofApp::setup(){	
	ofSetOrientation(OF_ORIENTATION_DEFAULT);
	ofSetFrameRate(30);
	ofSetFullscreen(true);
	ofSetCircleResolution(200.0f);
	ofEnableAlphaBlending();
	
	this->gui.init();
	
	if (ofGetWidth() < 1024) {
		this->gui.setZoom(0.5f);
	}
	
	this->startButton = shared_ptr<StartButton>(new StartButton());
	this->gui.add(this->startButton);

	this->countdown = shared_ptr<Countdown>(new Countdown());
	this->countdown->setBounds(this->startButton->getBounds());
	this->gui.add(this->countdown);
	
	this->recording = shared_ptr<Recording>(new Recording());
	this->recording->setBounds(this->countdown->getBounds());
	this->gui.add(this->recording);
	
	this->loadingProgress = shared_ptr<LoadingProgress>(new LoadingProgress());
	this->loadingProgress->setBounds(ofRectangle(0, 0, 1536, 2048));
	this->gui.add(this->loadingProgress);
	
	ofAddListener(this->startButton->onHitSimple, this, &ofApp::gotoCountdown);
	ofAddListener(this->countdown->onCountdownOver, this, &ofApp::gotoRecording);
	ofAddListener(this->recording->onRecordingComplete, this, &ofApp::gotoWaiting);
	
	this->state = State_Waiting;
	
	this->oscSender.setup("192.168.0.2", 3456);
	this->oscReceiver.setup(4444);
}


//--------------------------------------------------------------
void ofApp::update(){
    auto loadingEnabled = (ofGetElapsedTimef() - this->lastLoadingProgressMessage) < 5.0f;
    this->loadingProgress->setEnabled(loadingEnabled);
    
    this->startButton->setEnabled(this->state == State_Waiting && !loadingEnabled);
    this->countdown->setEnabled(this->state == State_CountingDown && !loadingEnabled);
    this->recording->setEnabled(this->state == State_Recording && !loadingEnabled);
    
	this->gui.update();
	
	while(this->oscReceiver.hasWaitingMessages()) {
		ofxOscMessage msg;
		this->oscReceiver.getNextMessage(&msg);
		if(msg.getAddress() == "/loading/progress") {
			this->loadingProgress->setProgress(msg.getArgAsFloat(0));
			this->lastLoadingProgressMessage = ofGetElapsedTimef();
		}
	}

	
}

//--------------------------------------------------------------
void ofApp::draw(){
	ofBackgroundHex(0xf6f6ec);
	this->gui.draw();
	ofPopMatrix();
}

//--------------------------------------------------------------
void ofApp::exit(){
    
}

//--------------------------------------------------------------
void ofApp::touchDown(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchMoved(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchUp(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchDoubleTap(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchCancelled(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::gotoCountdown() {
	this->state = State_CountingDown;
	this->countdown->reset();
}

//--------------------------------------------------------------
void ofApp::gotoRecording() {
	this->state = State_Recording;
	this->recording->reset();
	
	ofxOscMessage msg;
	msg.setAddress("/record");
	oscSender.sendMessage(msg);
}

//--------------------------------------------------------------
void ofApp::gotoWaiting() {
	this->state = State_Waiting;
	this->startButton->reset();
}
