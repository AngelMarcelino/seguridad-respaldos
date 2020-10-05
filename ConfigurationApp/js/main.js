const {dialog} = require('electron').remote
const fs = require('fs');
const { config } = require('process');


const inputs =  Array.from(document.getElementsByTagName('input'));

const backupConfigurationFileName = 'backup-configuration.json'

const addDirBtn = document.getElementById('add-dir-btn');
const saveBtn = document.getElementById('save-btn')

addDirBtn.addEventListener('click', function() {
  const foldersToBackup = dialog.showOpenDialogSync({
    properties: ['openDirectory', 'multiSelections'],
  });
  console.log(foldersToBackup);
  foldersToBackup.forEach(folderToBackup => {
    addDirList(folderToBackup);
  });
});

const pathListUl = document.getElementById('backup-folder-list');
function addDirList(path) {
  const newFolderToBackupLi = createFolderToBackupListElement(path);
  pathListUl.appendChild(newFolderToBackupLi);
}

function createFolderToBackupListElement(text) {
  const li = document.createElement('li');
  li.classList.add('list-group-item');
  li.innerHTML = text;
  const button = document.createElement('button');
  button.setAttribute('class', 'btn btn-danger float-right');
  button.innerHTML = 'Eliminar'
  button.addEventListener('click', (event) => deleteFolderToBackup(event));
  li.appendChild(button);
  return li;
}


function deleteFolderToBackup(event) {
  event.target.parentElement.remove();
}

saveBtn.addEventListener('click', function(event) {
  const values = getValues();
  const foldersToBackup = getFoldersToBackup();
  const configurationObject = createConfigurationObject(values, foldersToBackup);
  saveConfigurationObject(configurationObject);
});

function getValues() {
  const values = inputs.reduce(
    (accum, current) => 
      (accum[current.name] = current.name.startsWith('interval') ? parseFloat(current.value) : current.value, accum),
      {}
    );
  console.log(values);
  return values;
}

function getFoldersToBackup() {
  const result = [];
  for (let i = 0; i < pathListUl.children.length; i++) {
    result.push(pathListUl.children[i].textContent);
  }
  console.log(result);
  return result;
}

function createConfigurationObject(scalarValues, pathsToBackup) {
  const timeSpanInMinutes = convertTimeSpanToMinutes(scalarValues.interval_days, scalarValues.interval_hours, scalarValues.interval_minutes);
  const result = {};
  result.BackupIntervalInMinutes = timeSpanInMinutes;
  result.DestinationFtpUrl = scalarValues.ftp_uri;
  result.FtpUserName = scalarValues.ftp_user;
  result.FtpPassword = scalarValues.ftp_password;
  result.FoldersToBackup = pathsToBackup;
  return result;
}

function convertTimeSpanToMinutes(days, hours, minutes) {
  return days * 24 * 60 + hours * 60 + minutes;
}

function convertTimeSpanInMinutesToTimeSpan(timeSpanMinutes) {
  const days = Math.floor((timeSpanMinutes / 24) / 60);
  timeSpanMinutes -= days * 24 * 60;
  const hours = Math.floor((timeSpanMinutes / 60));
  timeSpanMinutes -= hours * 60;
  const minutes = timeSpanMinutes;
  return [days, hours, minutes];
}

function saveConfigurationObject(configurationObject) {
  const jsonString = JSON.stringify(configurationObject, undefined, 2);
  fs.writeFileSync(backupConfigurationFileName, jsonString);
}

function loadPreviousData() {
  const configObj = getValuesFromFile();
  inputs.forEach(input => {
    input.value = configObj[input.name];
  });
}

function getValuesFromFile() {
  const text = fs.readFileSync(backupConfigurationFileName, { encoding: 'utf8' });
  const result = {};
  console.log(text);
  const configuration = JSON.parse(text);
  result.ftp_uri = configuration.DestinationFtpUrl;
  result.ftp_user = configuration.FtpUserName;
  result.ftp_password = configuration.FtpPassword;
  result.pathsToBackup = configuration.FoldersToBackup;
  const timeSpanParts = convertTimeSpanInMinutesToTimeSpan(configuration.BackupIntervalInMinutes)
  result.interval_days = timeSpanParts[0];
  result.interval_hours = timeSpanParts[1];
  result.interval_minutes = timeSpanParts[2];
  return result;
}

loadPreviousData();
