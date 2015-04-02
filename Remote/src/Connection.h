#pragma once

#include "ofMain.h"

#include "ofxMySQL.h"
#include "ofxOsc.h"
#include "ofxKCTouchGui.h"
#include "ofxAssets.h"


class Connection : public ofxKCTouchGui::Element {
public:
	Connection();
	ofxMySQL & getConnection();
	shared_ptr<ofxOscSender> getOscSender();
protected:
	void update();
	void draw();
	ofxMySQL database;
	shared_ptr<ofxOscSender> osc;
	
	string hostname, username, password, dbname;
};