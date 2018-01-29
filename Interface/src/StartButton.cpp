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
    this->resetTime = chrono::system_clock::now();
}

//----------
void StartButton::draw() {
    auto now = chrono::system_clock::now();
    auto runTimeRaw = now - this->resetTime;
    auto runTime = (float)  chrono::duration_cast<chrono::milliseconds>(runTimeRaw).count() / 1000.0f;
    
	float phase = runTime / 4.0f * TWO_PI;
	
	//red circle
	ofPushStyle();
	ofSetColor(0x9d,0x0b,0x0e);
	float r = this->getBounds().getWidth() / 2.0f;
	ofDrawCircle(r, r, r);
	ofPopStyle();
	
	if (runTime < 1.0f) {
		//inner circle
		ofPushStyle();
		ofSetColor(0xf6, 0xf6, 0xec);
		ofDrawCircle(r, r, ofMap(runTime, 0, 1, r - 30, 0, true));
		ofPopStyle();
	} else {
		if(this->getTouchCount() > 0) {
			auto & touchDown = image("touch");
			touchDown.draw((this->getBounds().getWidth() - touchDown.getWidth()) / 2.0f, (this->getBounds().getHeight() - touchDown.getHeight()) / 2.0f);
		} else {
			//text
			ofPushStyle();
			ofSetColor(255, 255.0f * (sin(phase) + 1.0f) / 2.0f);
			auto & touch = image("touch");
			touch.draw((this->getBounds().getWidth() - touch.getWidth()) / 2.0f, (this->getBounds().getHeight() - touch.getHeight()) / 2.0f);
			ofPopStyle();
		}
		
		//black dot
		ofPushStyle();
		ofSetColor(0);
		ofDrawCircle(
				 (-sin(phase) + 1.0f) * this->getBounds().getWidth() / 2.0f,
				 (cos(phase) + 1.0f) * this->getBounds().getHeight() / 2.0f,
				 10.0f);
		ofPopStyle();
		
		//draw the under message offset
		image("touch_under").draw(ofVec2f(425, 1718) - this->getBounds().getTopLeft());
	}
}
