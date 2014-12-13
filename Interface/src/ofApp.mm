#include "ofApp.h"

//--------------------------------------------------------------
void ofApp::setup(){	
	ofSetOrientation(OF_ORIENTATION_DEFAULT);
	ofSetFrameRate(30);
	ofSetFullscreen(true);
	ofSetCircleResolution(200.0f);
	ofEnableAlphaBlending();
	
	this->gui.init(ofGetWidth() > 1024 ? 1.0f : 0.5f);
	
	this->startButton = shared_ptr<StartButton>(new StartButton());
	this->gui.add(this->startButton);

	this->countdown = shared_ptr<Countdown>(new Countdown());
	this->countdown->setBounds(this->startButton->getBounds());
	this->gui.add(this->countdown);
	
	this->recording = shared_ptr<Recording>(new Recording());
	this->recording->setBounds(this->countdown->getBounds());
	this->gui.add(this->recording);
	
	ofAddListener(this->startButton->onHitSimple, this, &ofApp::gotoCountdown);
	ofAddListener(this->countdown->onCountdownOver, this, &ofApp::gotoRecording);
	ofAddListener(this->recording->onRecordingComplete, this, &ofApp::gotoWaiting);
	this->fbo.allocate(ofGetWidth() * RES_MULT, ofGetHeight() * RES_MULT);
	
	this->state = State_Waiting;
	
	this->osc.setup("192.168.1.42", 3456);
}


//--------------------------------------------------------------
void ofApp::update(){
	this->gui.update();
	
	this->startButton->setEnabled(this->state == State_Waiting);
	this->countdown->setEnabled(this->state == State_CountingDown);
	this->recording->setEnabled(this->state == State_Recording);
}

//--------------------------------------------------------------
void ofApp::draw(){
	this->fbo.begin();
	ofPushMatrix();
	ofScale(RES_MULT, RES_MULT);
	ofBackgroundHex(0xf6f6ec);
	this->gui.draw();
	ofPopMatrix();
	this->fbo.end();
	
	this->fbo.draw(0,0,ofGetWidth(), ofGetHeight());
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
	osc.sendMessage(msg);
}

//--------------------------------------------------------------
void ofApp::gotoWaiting() {
	this->state = State_Waiting;
	this->startButton->reset();
}