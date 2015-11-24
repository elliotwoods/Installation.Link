#pragma once

#include "ofxKCTouchGui.h"

class LoadingProgress : public ofxKCTouchGui::Element {
public:
	LoadingProgress();
	void setProgress(float);
	float getProgress() const;
protected:
	void draw();
	float progress;
};