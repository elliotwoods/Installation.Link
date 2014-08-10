#include "Canvas.h"

//----------
Canvas::Canvas() {
	this->selectedCorner = 0;
	
	this->setWorkspaceAspect(1280.0f / 800.0f);
	ofAddListener(this->onDraw, this, &Canvas::draw);
	ofAddListener(this->onDrawWorkspace, this, &Canvas::drawWorkspace);
	ofAddListener(this->onHit, this, &Canvas::touchHit);
}

//----------
void Canvas::setConnection(shared_ptr<Connection> connection) {
	this->connection = connection;
	this->refresh();
}

//----------
void Canvas::addNew() {
	if (this->connection) {
		shared_ptr<Quad> newQuad(new Quad(this->getNextFreeIndex(), 0, 0));
		
		newQuad->corners[0] = ofVec3f(10, 10, 0) * this->transform.getInverse();
		newQuad->corners[1] = ofVec3f(this->getBounds().getWidth() - 10, 10, 0) * this->transform.getInverse();
		newQuad->corners[2] = ofVec3f(this->getBounds().getWidth() - 10, this->getBounds().getHeight() -10, 0) * this->transform.getInverse();
		newQuad->corners[3] = ofVec3f(10, this->getBounds().getHeight() -10, 0) * this->transform.getInverse();
		
		newQuad->update();
		
		this->quads.insert(pair<int, shared_ptr<Quad> >(newQuad->index, newQuad));
		
		auto & database = this->connection->getConnection();
		database.insert("quads", newQuad->getDatabaseRow());
	}
}

//----------
void Canvas::deleteSelection() {
	if (this->selection) {
		this->quads.erase(this->selection->index);
		if (this->connection) {
			auto & database = this->connection->getConnection();
			database.deleteRow("quads", "id=" + ofToString(this->selection->index));
		}
		this->selection.reset();
	}
}

//----------
shared_ptr<Quad> Canvas::getSelection() {
	return this->selection;
}

//----------
int Canvas::getNextFreeIndex() const {
	int nextFreeIndex = 0;
	for(auto quad : this->quads) {
		if (quad.first >= nextFreeIndex) {
			nextFreeIndex++;
		}
	}
	return nextFreeIndex;
}

//----------
void Canvas::draw() {
	if (this->selection) {
		for (int i=0; i<4; i++) {
			auto cornerPosition = (ofVec3f) this->selection->corners[i] * this->transform;

			ofPushStyle();
			if (this->selectedCorner == i) {
				ofSetLineWidth(0.0f);
				ofFill();
			} else {
				ofSetLineWidth(1.0f);
				ofNoFill();
			}
			ofCircle(cornerPosition, 10.0f);
			ofPopStyle();
		}
	}
	
	stringstream status;
	for(auto quad : this->quads) {
		status << * quad.second << endl;
	}
	ofDrawBitmapString(status.str(), 10, 10);
}

//----------
void Canvas::drawWorkspace() {
	for(auto quad : this->quads) {
		quad.second->draw(quad.second == this->selection);
	}
}

//----------
void Canvas::touchHit(ofxKCTouchGui::Touch & touch) {
	ofVec3f touchLocation = (ofVec2f &) touch;
	touchLocation -= this->getBounds().getTopLeft();
	
	for(auto it = this->quads.rbegin(); it != this->quads.rend(); it++) {
		auto quad = it->second;
		auto touchInQuadSpace = touchLocation * this->transform.getInverse() * quad->getTransform().getInverse();
		
		if (ofRectangle(-1, -1, 2, 2).inside(touchInQuadSpace)) {
			this->selection = quad;
			
			int closestCornerIndex = -1;
			float closestCornerDistance = std::numeric_limits<float>::max();
			for(int i=0; i<4; i++) {
				auto cornerDistance = (touchInQuadSpace - quad->sourceCorners[i]).lengthSquared();
				if (cornerDistance < closestCornerDistance) {
					closestCornerDistance = cornerDistance;
					closestCornerIndex = i;
				}
			}
			if (closestCornerIndex != -1) {
				this->selectedCorner = closestCornerIndex;
			}
			return;
		}
	}
	
	this->selection.reset();
}

//----------
void Canvas::refresh() {
	if (this->connection) {
		auto & database = this->connection->getConnection();
		if (database.isConnected()) {
			auto quadTableResult = database.select("quads", "*", "ORDER BY id ASC");
			
			QuadMap resultQuads;
			
			for(auto & quadRowResult : quadTableResult) {
				shared_ptr<Quad> resultQuad(new Quad(quadRowResult));
				resultQuads[resultQuad->index] = resultQuad;
			}
			
			this->quads = resultQuads;
		}
	}
}

//----------
void Canvas::updateSelectionOnServer() const {
	if (this->selection) {
		auto row = this->selection->getDatabaseRow();
		auto & database = this->connection->getConnection();
		database.update("quads", row, "id=" + ofToString(this->selection->index));
	}
}

//----------
void Canvas::touchMoved(ofxKCTouchGui::Touch & touch) {
	auto touchMovementInWorkspace = (ofVec3f) touch.getPosition() * this->transform.getInverse() - (ofVec3f) touch.getPreviousPosition() * this->transform.getInverse();
	if (this->selection && this->getTouchCount() == 1) {
		ofVec2f newCorners[4];
		for(int i=0; i<4; i++) {
			newCorners[i] = this->selection->corners[i];
		}
		newCorners[this->selectedCorner] += touchMovementInWorkspace;
		if (ofxHomography::findHomography(Quad::sourceCorners, newCorners).getInverse().isValid()) {
			for(int i=0; i<4; i++) {
				this->selection->corners[i] = newCorners[i];
			}
			this->selection->update();
			this->updateSelectionOnServer();
		}
	} else if (this->selection && this->getTouchCount() == 3) {
		for(int i=0; i<4; i++) {
			this->selection->corners[i] += touchMovementInWorkspace;
		}
		this->selection->update();
		this->updateSelectionOnServer();
	} else if (this->getTouchCount() != 2) {
		this->viewPosition -= touchMovementInWorkspace;
	}
}