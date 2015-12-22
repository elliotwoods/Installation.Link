#include "Quad.h"
#include "ofxAssets.h"

//----------
ofVec2f * Quad::sourceCorners = NULL;

//----------
Quad::Quad(int index, int iProjector, int iType) {
	this->index = index;
	this->iProjector = iProjector;
	this->iType = iType;
	
	this->update();
}

//----------
Quad::Quad(const ofxMySQL::Row & row) {
	for(int i=0; i<4; i++) {
		auto cornerName = "corner" + ofToString(i);
		row.get(cornerName + "x", this->corners[i].x);
		row.get(cornerName + "y", this->corners[i].y);
	}
	row.get("id", this->index);
	row.get("zOrder", this->zOrder);
	row.get("iProjector", this->iProjector);
	row.get("iType", this->iType);
	
	this->update();
}

//----------
void Quad::update() {
	this->init();
	this->homography = ofxHomography::findHomography(sourceCorners, corners);
	this->homography(2, 2) = 1.0f;
	
	this->meshOutline.clear();
	for(int i=0; i<4; i++) {
		this->meshOutline.addVertex(this->corners[i]);
	}
	this->meshOutline.setMode(OF_PRIMITIVE_LINE_LOOP);
	
	this->meshFill = this->meshOutline;
	this->meshFill.setMode(OF_PRIMITIVE_TRIANGLE_FAN);
}

//----------
void Quad::draw(bool selected) {
	ofPushStyle();
	ofNoFill();
	ofSetLineWidth(2.0f);
	this->meshOutline.draw();
	
	ofFill();
	ofSetColor(selected ? 200 : 20);
	this->meshFill.draw();
	ofPopStyle();

	ofPushMatrix();
	ofMultMatrix(this->homography);
	ofScale(2.0f / 500.0f, -2.0f / 500.0f);
	ofPushStyle();
	ofSetColor(selected ? 0 : 255);
	ofxAssets::font("ofxKCTouchGui2::swisop3", 100).drawStringAsShapes(TypeSelection::X().getCaptionForIndex(this->iType), -250, 200);
	ofPopStyle();
	
	if (selected) {
		ofLine(-0.05f, 0.0f, 0.05f, 0.0f);
		ofLine(0.0f, -0.05f, 0.0f, 0.05f);
	}
	ofPopMatrix();
}

//----------
ofMatrix4x4 Quad::getTransform() const {
	return this->homography;
}

//----------
ofxMySQL::Row Quad::getDatabaseRow() const {
	ofxMySQL::Row row;
	for(int i=0; i<4; i++) {
		auto cornerName = "corner" + ofToString(i);
		row.set(cornerName + "x", this->corners[i].x);
		row.set(cornerName + "y", this->corners[i].y);
	}
	row.set("id", this->index);
	row.set("zOrder", this->zOrder);
	row.set("iProjector", this->iProjector);
	row.set("iType", this->iType);
	return row;
}

//----------
void Quad::init() {
	if (!this->sourceCorners) {
		this->sourceCorners = new ofVec2f[4];
		this->sourceCorners[0] = ofVec2f(-1, +1);
		this->sourceCorners[1] = ofVec2f(+1, +1);
		this->sourceCorners[2] = ofVec2f(+1, -1);
		this->sourceCorners[3] = ofVec2f(-1, -1);
	}
}

//----------
ostream & operator<<(ostream & os, const Quad & quad) {
	os << "[" << quad.index << "] ";
	for(int i=0; i<4; i++) {
		os << quad.corners[i] << " : ";
	}
	return os;
}