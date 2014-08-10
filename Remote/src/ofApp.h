#pragma once

#include "ofMain.h"

#include "ofxiOS.h"
#include "ofxiOSExtras.h"
#include "ofxKCTouchGui.h"

#include "Connection.h"
#include "Canvas.h"

class ofApp : public ofxiOSApp{
    
public:
	void setup();
	void update();
	void draw();

	void exit();

	void touchDown(ofTouchEventArgs & touch);
	void touchMoved(ofTouchEventArgs & touch);
	void touchUp(ofTouchEventArgs & touch);
	void touchDoubleTap(ofTouchEventArgs & touch);
	void touchCancelled(ofTouchEventArgs & touch);

	void lostFocus();
	void gotFocus();
	void gotMemoryWarning();
	void deviceOrientationChanged(int newOrientation);

	shared_ptr<ofxKCTouchGui::Elements::Button> addButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> deleteButton;
	
	shared_ptr<Connection> connection;
	shared_ptr<Canvas> canvas;
	
	ofxKCTouchGui::Controller gui;
};


