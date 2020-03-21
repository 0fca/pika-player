const input = document.getElementById("file_uploads");

function removeFromFiles(name) {
    let files = input.files;

    for (let i = 0; i < files.length; i++) {
        if (name === files[i].name) {
            files[i] = "";
            break;
        }
    }
}

function getSummarySize() {
    let files = input.files;
    let sum = 0;

    for (let i = 0; i < files.length; i++) {
        sum += files[i].size;
    }
    return sum;
}



function returnFileSize(number) {
    if (number < 1024) {
        return number + 'bytes';
    } else if (number >= 1024 && number < 1048576) {
        return (number / 1024).toFixed(1) + 'kB';
    } else if (number >= 1048576) {
        return (number / 1048576).toFixed(1) + 'MB';
    }
}

function getFileList() {
    let files = input.files;
    let result = [];

    for (let i = 0; i < files.length; i++) {
        result[i] = files[i].name;
    }
    return result;
}

function deleteAllFiles() {
    let fileInput = document.getElementById("file_uploads");
    fileInput.value = "";
    let messageLabel = document.getElementById("preview");
    messageLabel.innerText = "No files to be uploaded.";
    resetListOnView();
}

function resetListOnView(){
    let files = input.files;

    for (let i = 0; i < files.length; i++) {
            files[i] = "";
    }
}

