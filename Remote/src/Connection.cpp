#include "Connection.h"
#include "Credentials.h"
//---------
Connection::Connection() {
	ofAddListener(this->onUpdate, this, &Connection::update);
	ofAddListener(this->onDraw, this, &Connection::draw);
	
	this->hostname = "192.168.1.3";
	this->username = DB_USER;
	this->password = DB_PASSWORD;
	this->dbname = "link";
}

//---------
ofxMySQL & Connection::getConnection() {
	return this->database;
}

//---------
shared_ptr<ofxOscSender> Connection::getOscSender() {
	return this->osc;
}

//---------
void Connection::update() {
	if (!this->database.isConnected()) {
		this->database.connect(this->hostname, this->username, this->password, this->dbname);
		this->osc.reset();
	}
	if(!this->osc) {
		auto osc = shared_ptr<ofxOscSender>(new ofxOscSender());
		try {
			osc->setup(this->hostname, 4456);
			this->osc = osc;
		} catch(std::exception e) {
			cout << e.what() << endl;
		}
	}
}

//---------
void Connection::draw() {
	auto & font = ofxAssets::font("ofxKCTouchGui2::swisop3", 50);
	
	ofPushStyle();
	if (this->database.isConnected()) {
		font.drawString("Connected", 70, 55);
		ofSetColor(100, 255, 100);
	} else {
		ofNoFill();
		ofSetLineWidth(2.0f);
		font.drawString("No connection", 70, 55);
		ofSetColor(255, 100, 100);
	}
	ofCircle(33, 33, 20);
	ofPopStyle();
}
