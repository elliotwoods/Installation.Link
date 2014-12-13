#pragma once

#include "ofxKCTouchGui.h"

class Countdown : public ofxKCTouchGui::Element {
public:
	Countdown();
	void reset();
	ofEvent<void> onCountdownOver;
protected:
	void draw();
	float resetTime;
};