#include "LoadingProgress.h"

//----------
LoadingProgress::LoadingProgress() {
	ofAddListener(this->onDraw, this, &LoadingProgress::draw);
	
	this->progress = 1.0f;
}

//----------
void LoadingProgress::setProgress(float progress) {
	this->progress = progress;
}

//----------
float LoadingProgress::getProgress() const {
	return this->progress;
}

//----------
void LoadingProgress::draw() {
	ofPushStyle();
	ofSetColor(255,0,0);
	ofDrawRectangle(0, 0, this->getBounds().getWidth(), progress * this->getBounds().getHeight());
	
	ofSetColor(0);
	ofPushMatrix();
	ofTranslate(20.0f, this->getBounds().getHeight() / 2.0f);
	ofScale(4.0f, 4.0f);
	ofDrawBitmapString(string("Loading..."), 0, 0);
	ofPopMatrix();
	ofPopStyle();
}