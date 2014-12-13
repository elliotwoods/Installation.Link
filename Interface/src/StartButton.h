#pragma once

#include "ofxKCTouchGui.h"

class StartButton : public ofxKCTouchGui::Element {
public:
	StartButton();
	void reset();
protected:
	void draw();
	float resetTime;
};