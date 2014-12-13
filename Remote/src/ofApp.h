#pragma once

#include "ofMain.h"

#include "Connection.h"
#include "Canvas.h"
#include "TypeSelection.h"

#include "ofxiOS.h"
#include "ofxiOSExtras.h"
#include "ofxKCTouchGui.h"
#include "ofxOsc.h"

class ViewModeSelection : public ofxKCTouchGui::Elements::MultiSelect { };

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

	void noteAction();
	void pushOsc();
	
	shared_ptr<ofxKCTouchGui::Elements::Button> addButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> clearSelectionButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> deleteButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> sendToBackButton;
	
	shared_ptr<ofxKCTouchGui::Elements::Button> flipHorizontalButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> flipVerticalButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> rotateRightButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> snapPointButton;
	shared_ptr<ofxKCTouchGui::Elements::Button> resetQuadButton;
	
	shared_ptr<Connection> connection;
	shared_ptr<Canvas> canvas;
	shared_ptr<ProjectorSelection> projectorSelection;
	shared_ptr<TypeSelection> typeSelection;
	shared_ptr<ViewModeSelection> viewModeSelection;
	
	ofxKCTouchGui::Controller gui;
	
	float timeOfLastAction;
};


