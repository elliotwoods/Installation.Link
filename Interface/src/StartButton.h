#pragma once

#include "ofxKCTouchGui.h"

class StartButton : public ofxKCTouchGui::Element {
public:
	StartButton();
	void reset();
protected:
	void draw();
	chrono::time_point<chrono::system_clock> resetTime = chrono::system_clock::now();
};
