#include "Connection.h"
#include "Credentials.h"
//---------
Connection::Connection() {
	ofAddListener(this->onUpdate, this, &Connection::update);
	ofAddListener(this->onDraw, this, &Connection::draw);
	
	this->hostname = "192.168.1.4";
	this->username = DB_USER;
	this->password = DB_PASSWORD;
	this->dbname = "link";
}

//---------
ofxMySQL & Connection::getConnection() {
	return this->database;
}

//---------
ofxOscSender & Connection::getOscSender() {
	return this->osc;
}

//---------
void Connection::update() {
	if (!this->database.isConnected()) {
		this->database.connect(this->hostname, this->username, this->password, this->dbname);
		this->osc.setup(this->hostname, 4456);
	}
}

//---------
void Connection::draw() {
	auto & font = ofxAssets::font("swisop3", 50);
	
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
