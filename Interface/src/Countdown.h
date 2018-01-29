#pragma once

#include "ofxKCTouchGui.h"

class Countdown : public ofxKCTouchGui::Element {
public:
	Countdown();
	void reset();
	ofEvent<void> onCountdownOver;
protected:
	void update();
	void draw();
    chrono::time_point<chrono::system_clock> resetTime = chrono::system_clock::now();
};
