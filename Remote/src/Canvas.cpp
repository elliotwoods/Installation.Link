#include "Canvas.h"

//----------
Canvas::Canvas() {
	this->selectedCorner = 0;
	this->enableUpdateQuad = true;
	
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
void Canvas::setProjectorSelection(shared_ptr<ProjectorSelection> projectorSelection) {
	if (this->projectorSelection) {
		ofRemoveListener(this->projectorSelection->onSelectIndex, this, &Canvas::callbackProjectorSelect);
	}
	this->projectorSelection = projectorSelection;
	ofAddListener(projectorSelection->onSelectIndex, this, &Canvas::callbackProjectorSelect);
}

//----------
void Canvas::setTypeSelection(shared_ptr<TypeSelection> typeSelection) {
	if (this->typeSelection) {
		ofRemoveListener(this->typeSelection->onTypeIndexSelection, this, &Canvas::callbackTypeSelect);
	}
	this->typeSelection = typeSelection;
	ofAddListener(typeSelection->onTypeIndexSelection, this, &Canvas::callbackTypeSelect);
	this->refresh();
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
			
			int cacheSelection = -1;
			if (this->selection) {
				cacheSelection = selection->index;
			}
			this->quads = resultQuads;
			if (cacheSelection != -1) {
				this->setSelection(cacheSelection);
			}
		}
		
		if (this->typeSelection) {
			this->enableUpdateQuad = false;
			this->typeSelection->refresh();
			this->enableUpdateQuad = true;
		}
	}
}

//----------
void Canvas::addNew() {
	if (this->connection) {
		shared_ptr<Quad> newQuad(new Quad(this->getNextFreeIndex(), this->projectorSelection->getSelectionIndex(), this->typeSelection->getSelectionDatabaseIndex()));
		
		float scaleOfNewInView = 0.5f;
		const float left = this->getBounds().getWidth() / 2.0f - this->getBounds().getWidth() / 2.0f * scaleOfNewInView;
		const float right = this->getBounds().getWidth() / 2.0f + this->getBounds().getWidth() / 2.0f * scaleOfNewInView;
		const float top = this->getBounds().getHeight() / 2.0f - this->getBounds().getHeight() / 2.0f * scaleOfNewInView;
		const float bottom = this->getBounds().getHeight() / 2.0f + this->getBounds().getHeight() / 2.0f * scaleOfNewInView;
		newQuad->corners[0] = ofVec3f(left, top, 0) * this->transform.getInverse();
		newQuad->corners[1] = ofVec3f(right, top, 0) * this->transform.getInverse();
		newQuad->corners[2] = ofVec3f(right, bottom, 0) * this->transform.getInverse();
		newQuad->corners[3] = ofVec3f(left, bottom, 0) * this->transform.getInverse();
		
		newQuad->update();
		
		this->quads.insert(pair<int, shared_ptr<Quad> >(newQuad->index, newQuad));
		this->setSelection(newQuad);
		
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
void Canvas::bringSelectionToFront() {
	if (this->selection && this->connection) {
		auto cacheSelection = this->selection;
		this->deleteSelection();
		cacheSelection->index = this->getNextFreeIndex();
		this->quads.insert(pair<int, shared_ptr<Quad> >(cacheSelection->index, cacheSelection));
		this->setSelection(cacheSelection);
		this->connection->getConnection().insert("quads", cacheSelection->getDatabaseRow());
	}
}

//----------
shared_ptr<Quad> Canvas::getSelection() {
	return this->selection;
}

//----------
void Canvas::setSelection(int index) {
	auto findQuad = this->quads.find(index);
	if (findQuad != this->quads.end()) {
		this->setSelection(findQuad->second);
	} else {
		this->clearSelection();
	}
}

//----------
void Canvas::clearSelection() {
	this->selection.reset();
}

//----------
void Canvas::setSelection(shared_ptr<Quad> quad) {
	this->selection = quad;
	this->enableUpdateQuad = false;
	this->typeSelection->setSelectionByDatabaseIndex(quad->iType);
	this->enableUpdateQuad = true;
}

//----------
ofVec2f Canvas::getSelectedCornerLocation() const {
	if (this->selection) {
		return this->selection->corners[this->selectedCorner];
	} else {
		return ofVec2f();
	}
}

//----------
ofMatrix4x4 Canvas::getViewTransform() const {
	auto transform = this->transform;
	
	ofMatrix4x4 lastTransformPart;
	lastTransformPart.translate(1.0f, 1.0f, 0.0f);
	lastTransformPart.scale(this->getBounds().getWidth() / 2.0f, this->getBounds().getHeight() / 2.0f, 1.0f);
	lastTransformPart.scale(this->getBounds().getHeight() / this->getBounds().getWidth(), 1.0f, 1.0f);
	
	return transform * lastTransformPart.getInverse();
}

//----------
int Canvas::getNextFreeIndex() const {
	int nextFreeIndex = 0;
	for(auto quad : this->quads) {
		if (quad.first >= nextFreeIndex) {
			nextFreeIndex = quad.first + 1;
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
			ofFill();
			ofSetLineWidth(0.0f);
			if (this->selectedCorner == i) {
				ofSetColor(255);
			} else {
				ofSetColor(0);
				
			}
			ofCircle(cornerPosition, 30.0f);
			ofNoFill();
			ofSetLineWidth(2.0f);
			ofSetColor(255);
			ofCircle(cornerPosition, 30.0f);
			ofPopStyle();
		}
	}
}

//----------
void Canvas::drawWorkspace() {
	for(auto quad : this->quads) {
		if (quad.second->iProjector == projectorSelection->getSelectionIndex()) {
			quad.second->draw(quad.second == this->selection);
		}
	}
}

//----------
void Canvas::touchHit(ofxKCTouchGui::Touch & touch) {
	ofVec3f touchLocation = (ofVec2f &) touch;
	touchLocation -= this->getBounds().getTopLeft();
	
	for(auto it = this->quads.rbegin(); it != this->quads.rend(); it++) {
		auto quad = it->second;
		if (quad->iProjector != this->projectorSelection->getSelectionIndex()) {
			continue;
		}
		auto touchInQuadSpace = touchLocation * this->transform.getInverse() * quad->getTransform().getInverse();
		
		auto rectangle = quad == this->selection ? ofRectangle(-1.5, -1.5, 3, 3) : ofRectangle(-1, -1, 2, 2);
		
		if (rectangle.inside(touchInQuadSpace)) {
			this->setSelection(quad);
			
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
void Canvas::updateSelectionOnServer() const {
	if (this->selection && this->enableUpdateQuad) {
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
			this->selection->corners[i] += touchMovementInWorkspace / 3.0f;
		}
		this->selection->update();
		this->updateSelectionOnServer();
	} else if (this->getTouchCount() != 2) {
		this->viewPosition -= touchMovementInWorkspace / (float) this->getTouchCount();
	}
}

//----------
void Canvas::callbackProjectorSelect(int & index) {
	if (this->selection) {
		if (this->selection->iProjector != index) {
			this->clearSelection();
		};
	}
}

//----------
void Canvas::callbackTypeSelect(int & index) {
	if (this->selection) {
		this->selection->iType = index;
		this->updateSelectionOnServer();
	}
}