#include "Countdown.h"
#include "ofxAssets.h"

using namespace ofxAssets;

//----------
Countdown::Countdown() {
	ofAddListener(this->onDraw, this, &Countdown::draw);
	this->reset();
}

//----------
void Countdown::reset() {
	this->resetTime = ofGetElapsedTimef();
}

//----------
void Countdown::draw() {
	float runTime = ofGetElapsedTimef() - this->resetTime;
	
	ofColor red = ofColor(0x9d,0x0b,0x0e);
	ofColor black = ofColor(0);
	ofColor gray = ofColor(0x95);
	
	//outer circle
	ofPushStyle();
	ofSetColor(red.getLerped(black, ofClamp(runTime, 0, 1)));
	float r = this->getBounds().getWidth() / 2.0f;
	ofCircle(r, r, r);
	ofPopStyle();

	//inner circle
	ofPushStyle();
	ofSetColor(0xf6, 0xf6, 0xec);
	ofCircle(r, r, ofMap(runTime, 0, 1, 0, r - 30, true));
	ofPopStyle();
	
	if (runTime >= 5.0f) {
		ofNotifyEvent(this->onCountdownOver, this);
	} else if (runTime > 1.0f) {
		//text
		ofPushStyle();
		ofSetColor(255, (sin((runTime - 0.25f) * TWO_PI) + 1.0f) / 2.0f * 255.0f);
		auto & img = image("countdown" + ofToString(5 - (int) floor(runTime)));
		img.draw((this->getBounds().getWidth() - img.getWidth()) / 2.0f, (this->getBounds().getHeight() - img.getHeight()) / 2.0f);
		ofPopStyle();
		
		//progress
		ofPath path;
		path.moveTo(0, -r);
		float fullPhase = TWO_PI * (runTime - 1.0f) / 4.0f;
		for(float fraction = 0; fraction <= 1.0f; fraction += 0.01f) {
			path.lineTo(sin(fullPhase * fraction) * r, -cos(fullPhase * fraction) * r);
		}
		for(float fraction = 1.0; fraction >= 0.0f; fraction -= 0.01f) {
			path.lineTo(sin(fullPhase * fraction) * (r - 30), -cos(fullPhase * fraction)  * (r - 30));
		}
		path.lineTo(0, -r);
		path.close();
		path.setFillColor(gray);
		path.setFilled(true);
		
		ofPushStyle();
		ofSetLineWidth(0);
		path.draw(r, r);
		ofPopStyle();
	}

}