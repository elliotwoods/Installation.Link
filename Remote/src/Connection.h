#pragma once

#include "ofMain.h"

#include "ofxMySQL.h"
#include "ofxKCTouchGui.h"
#include "ofxAssets.h"

class Connection : public ofxKCTouchGui::Element {
public:
	Connection();
	ofxMySQL & getConnection();
protected:
	void update();
	void draw();
	ofxMySQL database;
	
	string hostname, username, password, dbname;
};