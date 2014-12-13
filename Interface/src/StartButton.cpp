#include "StartButton.h"

#include "ofxAssets.h"

using namespace ofxAssets;

#define SCREEN_WIDTH 768 * 2
#define SCREEN_HEIGHT 1024 * 2

//----------
StartButton::StartButton() {
	ofAddListener(this->onDraw, this, &StartButton::draw);
	this->setBounds(ofRectangle((SCREEN_WIDTH - 444)/ 2,(SCREEN_HEIGHT - 444) / 2,444,444));
	this->reset();
}

//----------
void StartButton::reset() {
	this->resetTime = ofGetElapsedTimef();
}

//----------
void StartButton::draw() {
	float runTime = ofGetElapsedTimef() - this->resetTime;
	float phase = runTime / 4.0f * TWO_PI;
	
	//red circle
	ofPushStyle();
	ofSetColor(0x9d,0x0b,0x0e);
	float r = this->getBounds().getWidth() / 2.0f;
	ofCircle(r, r, r);
	ofPopStyle();
	
	if (runTime < 1.0f) {
		//inner circle
		ofPushStyle();
		ofSetColor(0xf6, 0xf6, 0xec);
		ofCircle(r, r, ofMap(runTime, 0, 1, r - 30, 0, true));
		ofPopStyle();
	} else {
		//text
		ofPushStyle();
		ofSetColor(255, 255.0f * (sin(phase) + 1.0f) / 2.0f);
		auto & recordyourvideo = image("recordyourvideo");
		recordyourvideo.draw((this->getBounds().getWidth() - recordyourvideo.getWidth()) / 2.0f, (this->getBounds().getHeight() - recordyourvideo.getHeight()) / 2.0f);
		ofPopStyle();
		
		//black dot
		ofPushStyle();
		ofSetColor(0);
		ofCircle(
				 (-sin(phase) + 1.0f) * this->getBounds().getWidth() / 2.0f,
				 (cos(phase) + 1.0f) * this->getBounds().getHeight() / 2.0f,
				 10.0f);
		ofPopStyle();
	}
}