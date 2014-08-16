#include "TypeSelection.h"

//----------
TypeSelection * TypeSelection::singleton = NULL;

//----------
TypeSelection::TypeSelection() {
	ofAddListener(this->onUpdate, this, &TypeSelection::update);
	ofAddListener(this->onSelectCaption, this, &TypeSelection::callbackSelectCaption);
	TypeSelection::singleton = this;
}

//----------
void TypeSelection::setConnection(shared_ptr<Connection> connection) {
	this->connection = connection;
}

//----------
void TypeSelection::refresh() {
	if (this->connection) {
		auto & databse = this->connection->getConnection();
		if (databse.isConnected()) {
			auto cacheSelection = this->getSelectionIndex();
			this->clearOptions();
			this->databaseID.clear();
			
			auto result = databse.select("facetypes");
			for(auto resultRow : result) {
				this->addOption(resultRow["NameShort"]);
				this->databaseID.insert(pair<string, int>(resultRow["NameShort"], ofToInt(resultRow["id"])));
			}
			
			this->setSelection(cacheSelection);
		}
	}
}

//----------
int TypeSelection::getSelectionDatabaseIndex() const {
	auto findCaption = this->databaseID.find(this->getSelectionCaption());
	if (findCaption != this->databaseID.end()) {
		return findCaption->second;
	} else {
		return -1;
	}
}

//----------
string TypeSelection::getCaptionForIndex(int index) const {
	for(auto it : this->databaseID) {
		if (it.second == index) {
			return it.first;
		}
	}
	return "";
}

//----------
void TypeSelection::setSelectionByDatabaseIndex(int index) {
	for (auto it : this->databaseID) {
		if (it.second == index) {
			this->setSelection(it.first);
		}
	}
}

//----------
TypeSelection & TypeSelection::X() {
	return * singleton;
}

//----------
void TypeSelection::update() {
	
}

//----------
void TypeSelection::callbackSelectCaption(string & caption) {
	auto selectionIndex = this->getSelectionDatabaseIndex();
	ofNotifyEvent(this->onTypeIndexSelection, selectionIndex, this) ;
}