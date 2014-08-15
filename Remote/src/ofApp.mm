#include "ofApp.h"

//--------------------------------------------------------------
void ofApp::setup(){	
	ofSetOrientation(OF_ORIENTATION_90_RIGHT);//Set iOS to Orientation Landscape Right
	ofSetFrameRate(60);
	ofSetFullscreen(true);
	
	this->timeOfLastAction = 0.0f;
	
	this->gui.init();
	
	this->connection = shared_ptr<Connection>(new Connection());
	this->connection->setBounds(ofRectangle(20, 40, 480, 100));
	
	this->canvas = shared_ptr<Canvas>(new Canvas());
	this->canvas->setConnection(this->connection);
	this->canvas->setBounds(ofRectangle(0, 0, ofGetWidth(), ofGetHeight()));
	this->gui.add(this->canvas);
	this->gui.add(this->connection);
	
	ofAddListener(this->connection->onHitSimple, this->canvas.get(), &Canvas::refresh);
	ofAddListener(this->canvas->onHitSimple, this, &ofApp::noteAction);
	
	this->addButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("plus")));
	this->addButton->setBounds(ofRectangle(ofGetWidth() - 220, ofGetHeight() - 220, 150, 150));
	ofAddListener(this->addButton->onButtonHit, this->canvas.get(), &Canvas::addNew);
	this->gui.add(this->addButton);
	
	this->clearSelectionButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("tick")));
	this->clearSelectionButton->setBounds(ofRectangle(this->canvas->getBounds().x + 70, ofGetHeight() - 220, 150, 150));
	ofAddListener(this->clearSelectionButton->onButtonHit, this->canvas.get(), &Canvas::clearSelection);
	this->gui.add(this->clearSelectionButton);
	
	this->deleteButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("cross")));
	this->deleteButton->setBounds(ofRectangle(this->canvas->getBounds().x + 70, ofGetHeight() - 220 - 200, 150, 150));
	ofAddListener(this->deleteButton->onButtonHit, this->canvas.get(), &Canvas::deleteSelection);
	this->gui.add(this->deleteButton);
	
	this->bringToFrontButton = shared_ptr<ofxKCTouchGui::Elements::Button>(new ofxKCTouchGui::Elements::Button(ofxAssets::image("bringToFront")));
	this->bringToFrontButton->setBounds(ofRectangle(this->canvas->getBounds().x + 70, ofGetHeight() - 220 - 400, 150, 150));
	ofAddListener(this->bringToFrontButton->onButtonHit, this->canvas.get(), &Canvas::bringSelectionToFront);
	this->gui.add(this->bringToFrontButton);
	
	this->projectorSelection = shared_ptr<ProjectorSelection>(new ProjectorSelection());
	this->projectorSelection->setBounds(ofRectangle(20, 140, 460, 100));
	this->projectorSelection->addOption("0");
	this->projectorSelection->addOption("1");
	this->projectorSelection->addOption("2");
	this->projectorSelection->addOption("3");
	this->gui.add(this->projectorSelection);
	this->canvas->setProjectorSelection(this->projectorSelection);
	
	this->typeSelection = shared_ptr<TypeSelection>(new TypeSelection());
	this->typeSelection->setBounds(ofRectangle(20 + 460 + 20, 140, ofGetWidth() - (20 + 460 + 20) - 20, 100));
	this->typeSelection->setConnection(this->connection);
	this->gui.add(this->typeSelection);
	this->canvas->setTypeSelection(this->typeSelection);
	
	this->viewModeSelection = shared_ptr<ViewModeSelection>(new ViewModeSelection());
	this->viewModeSelection->addOption("Live");
	this->viewModeSelection->addOption("Edit");
	this->viewModeSelection->addOption("Grid");
	this->viewModeSelection->setBounds(ofRectangle(this->typeSelection->getBounds().x, 20, 400, 100));
	this->gui.add(this->viewModeSelection);
}


//--------------------------------------------------------------
void ofApp::update(){
	this->gui.update();
	
	this->clearSelectionButton->setEnabled(this->canvas->getSelection());
	this->deleteButton->setEnabled(this->canvas->getSelection());
	this->bringToFrontButton->setEnabled(this->canvas->getSelection());
	
	if (ofGetElapsedTimef() - timeOfLastAction > 1.0f) {
		this->canvas->refresh();
		this->noteAction();
	}
}

//--------------------------------------------------------------
void ofApp::draw(){
	ofBackgroundGradient(60, 0);
	
	this->gui.draw();
	
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
	this->noteAction();
}

//--------------------------------------------------------------
void ofApp::touchMoved(ofTouchEventArgs & touch){
	this->noteAction();
}

//--------------------------------------------------------------
void ofApp::touchUp(ofTouchEventArgs & touch){
	this->noteAction();
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

//--------------------------------------------------------------
void ofApp::noteAction() {
	this->timeOfLastAction = ofGetElapsedTimef();
	this->pushOsc();
}

void ofApp::pushOsc() {
	auto & osc = this->connection->getOscSender();
	ofxOscBundle bundle;
	{
		ofxOscMessage msg;
		msg.setAddress("/Cursor");
		auto selectedCorner = this->canvas->getSelectedCornerLocation();
		msg.addFloatArg(selectedCorner.x);
		msg.addFloatArg(selectedCorner.y);
		bundle.addMessage(msg);
	}
	{
		ofxOscMessage msg;
		msg.setAddress("/iProjector");
		msg.addIntArg(this->projectorSelection->getSelectionIndex());
		bundle.addMessage(msg);
	}
	{
		ofxOscMessage msg;
		msg.setAddress("/View/Mode");
		msg.addIntArg(this->viewModeSelection->getSelectionIndex());
		bundle.addMessage(msg);
	}
	{
		ofxOscMessage msg;
		msg.setAddress("/View/Transform");
		auto transform = this->canvas->getViewTransform();
		for(int i=0; i<16; i++) {
			msg.addFloatArg(transform.getPtr()[i]);
		}
		bundle.addMessage(msg);
	}
	osc.sendBundle(bundle);
}