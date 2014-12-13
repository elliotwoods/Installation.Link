#include "Recording.h"
#include "ofxAssets.h"

using namespace ofxAssets;

//----------
Recording::Recording() {
	ofAddListener(this->onDraw, this, &Recording::draw);
	this->reset();
}

//----------
void Recording::reset() {
	this->resetTime = ofGetElapsedTimef();
}

//----------
void Recording::draw() {
	float runTime = ofGetElapsedTimef() - this->resetTime;
	
	ofColor red = ofColor(0x9d,0x0b,0x0e);
	ofColor black = ofColor(0);
	ofColor gray = ofColor(0x95);
	
	//outer circle
	ofPushStyle();
	ofSetColor(gray);
	float r = this->getBounds().getWidth() / 2.0f;
	ofCircle(r, r, r);
	ofPopStyle();
	
	//inner circle
	ofPushStyle();
	ofSetColor(0xf6, 0xf6, 0xec);
	ofCircle(r, r, r - 30);
	ofPopStyle();

	//text
	ofPushStyle();
	ofEnableAlphaBlending();
	ofSetColor(255, 255.0f * (1.0f - (1.0f + cos(runTime * TWO_PI)) / 2.0f));
	auto & img = image("recordingvideo");
	img.draw((this->getBounds().getWidth() - img.getWidth()) / 2.0f, (this->getBounds().getHeight() - img.getHeight()) / 2.0f);
	ofPopStyle();
	
	//progress
	ofPath path;
	path.moveTo(0, -r);
	float fullPhase = TWO_PI * runTime / 4.0f;
	for(float fraction = 0; fraction <= 1.0f; fraction += 0.01f) {
		path.lineTo(sin(fullPhase * fraction) * r, -cos(fullPhase * fraction) * r);
	}
	for(float fraction = 1.0; fraction >= 0.0f; fraction -= 0.01f) {
		path.lineTo(sin(fullPhase * fraction) * (r - 30), -cos(fullPhase * fraction)  * (r - 30));
	}
	path.lineTo(0, -r);
	path.close();
	path.setFillColor(red);
	path.setFilled(true);
	
	ofPushStyle();
	ofSetLineWidth(0);
	path.draw(r, r);
	ofPopStyle();
	
	if (runTime > 4.0f) {
		ofNotifyEvent(this->onRecordingComplete, this);
	}
}