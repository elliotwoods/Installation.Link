#pragma once

#include "ofxHomography.h"
#include "ofxMySQL.h"

#include "ofMain.h"

struct Quad {
public:
	Quad(int index, int iProjector, int iType);
	Quad(const ofxMySQL::Row &);
	void update();
	void draw(bool selected = false);
	
	ofMatrix4x4 getTransform() const;
	ofxMySQL::Row getDatabaseRow() const;
	
	static ofVec2f * sourceCorners;
	ofVec2f corners[4]; /// TL, TR, BR, BL. Clockwise from top left
	int index;
	int zOrder;
	int iProjector;
	int iType;
	
protected:
	void init();
	ofMesh meshOutline;
	ofMesh meshFill;
	
	ofMatrix4x4 homography;
};

ostream & operator<<(ostream & os, const Quad &);