#pragma once

#include "ofxKCTouchGui.h"

class Recording : public ofxKCTouchGui::Element {
public:
	Recording();
	void reset();
	ofEvent<void> onRecordingComplete;
protected:
	void draw();
	float resetTime;
};