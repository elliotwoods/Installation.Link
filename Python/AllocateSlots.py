import csv
import os.path
import tqdm

from shutil import copyfile, rmtree

def readCsv(filename):
	data = []
	with open(filename, 'r') as csvfile:
		filenameReader = csv.reader(csvfile, delimiter=' ')
		for row in filenameReader:
			if len(row) == 1:
				data.append(row[0])
			else:
				data.append(row)
	return data

allFilenames = readCsv('allFilenames.csv')
filenamesPerFolder = readCsv('filenamesPerFolder.csv')
folderNames = readCsv('folderNames.csv')

blankFile = '../Recordingsblank.dds'
recordingsFolder = '../Recordings'

if not os.path.isfile(blankFile):
	raise Exception('Blank file not found')

if not os.path.isdir(recordingsFolder):
	raise Exception('Recordings folder not found')

def deleteFolders():
	recordingsFolderContents = os.listdir(recordingsFolder)
	print('Deleting folders')
	for i in tqdm.tqdm(range(len(recordingsFolderContents))):
		itemPath = recordingsFolder + '/' + recordingsFolderContents[i]
		if not os.path.isdir(itemPath):
			continue
		rmtree(itemPath)

def createFolders(useBlank = True):
	if not useBlank:
		raise Exception('Unsupported argument')

	print('Setting up {} folders'.format(len(folderNames)))

	for i in tqdm.tqdm(range(len(folderNames))):
		folderName = folderNames[i]
		if not os.path.exists(folderName):
			os.makedirs(folderName)
			missingFiles = filenamesPerFolder
		else:
			folderFiles = os.listdir(folderName)
			ddsFiles = list(filter(lambda ext: ext.endswith('.dds'), folderFiles))
			missingFiles = list(filter(lambda filename: filename not in ddsFiles, filenamesPerFolder))
		
		for filename in missingFiles:
			copyfile(blankFile, folderName + '\\' + filename)