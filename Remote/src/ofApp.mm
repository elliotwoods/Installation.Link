#include "ofApp.h"

//--------------------------------------------------------------
void ofApp::setup(){	
	ofSetOrientation(OF_ORIENTATION_90_RIGHT);//Set iOS to Orientation Landscape Right
	ofSetFrameRate(60);
	ofSetFullscreen(true);
	
	this->connection = shared_ptr<Connection>(new Connection());
	this->canvas = shared_ptr<Canvas>(new Canvas());
	this->canvas->setConnection(this->connection);
	
	this->gui.init();
	
	this->gui.add(this->canvas);
	this->canvas->setBounds(ofRectangle(500, 0, ofGetWidth() - 500, ofGetHeight()));
	
	this->addButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("plus")));
	this->addButton->setBounds(ofRectangle(ofGetWidth() - 220, ofGetHeight() - 220, 150, 150));
	ofAddListener(this->addButton->onButtonHit, this->canvas.get(), &Canvas::addNew);
	this->gui.add(this->addButton);
	
	this->deleteButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("cross")));
	this->deleteButton->setBounds(ofRectangle(this->canvas->getBounds().x + 220, ofGetHeight() - 220, 150, 150));
	ofAddListener(this->deleteButton->onButtonHit, this->canvas.get(), &Canvas::deleteSelection);
	this->gui.add(this->deleteButton);
	
	auto selectProjector = shared_ptr<ofxKCTouchGui::Elements::MultiSelect>(new ofxKCTouchGui::Elements::MultiSelect());
	selectProjector->setBounds(ofRectangle(20, 120, 460, 160));
	selectProjector->addOption("0");
	selectProjector->addOption("1");
	selectProjector->addOption("2");
	selectProjector->addOption("3");
	this->gui.add(selectProjector);
	
	auto selectType = shared_ptr<ofxKCTouchGui::Elements::MultiSelect>(new ofxKCTouchGui::Elements::MultiSelect());
	this->gui.add(selectType);
	
}


//--------------------------------------------------------------
void ofApp::update(){
	this->gui.update();
	
	this->addButton->setEnabled(!this->canvas->getSelection());
	this->deleteButton->setEnabled(this->canvas->getSelection());
}

//--------------------------------------------------------------
void ofApp::draw(){
	ofBackgroundGradient(60, 0);
	
	this->gui.draw();
	connection->drawStatus();
	
	stringstream debug;
	const auto & touches = this->gui.getTouches();
	for(auto & touch : touches) {
		debug << * touch.second << endl;
		touch.second->drawDebug();
	}
	ofDrawBitmapString(debug.str(), 20, 200);
}

//--------------------------------------------------------------
void ofApp::exit(){
    
}

//--------------------------------------------------------------
void ofApp::touchDown(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchMoved(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchUp(ofTouchEventArgs & touch){
	
}

//--------------------------------------------------------------
void ofApp::touchDoubleTap(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::touchCancelled(ofTouchEventArgs & touch){

}

//--------------------------------------------------------------
void ofApp::lostFocus(){
    
}

//--------------------------------------------------------------
void ofApp::gotFocus(){
    
}

//--------------------------------------------------------------
void ofApp::gotMemoryWarning(){
    
}

//--------------------------------------------------------------
void ofApp::deviceOrientationChanged(int newOrientation){
    
}
