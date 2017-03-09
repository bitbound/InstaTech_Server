function initComputerHub() {
    getAllComputerGroups();
    populateHubComputerGroups();
}

function searchComputerHub() {
    getAllComputerGroups();
    $("#tableComputerHub tbody").html("");
    var request = {
        "Type": "SearchComputerHub",
        "SearchBy": $("#selectSearchBy").val(),
        "SearchGroup": $("#selectComputerHubGroups").val(),
        "SearchString": $("#inputSearchComputers").val(),
        "AuthenticationToken": InstaTech.AuthenticationToken
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
}

function hubRowClicked(e) {
    if (!e.shiftKey && !e.ctrlKey) {
        $("#tableComputerHub tbody tr").removeClass("selected");
    }
    e.currentTarget.classList.add("selected");
    if (e.shiftKey) {
        var firstRow = $("#tableComputerHub tbody tr.selected").first();
        var lastRow = $("#tableComputerHub tbody tr.selected").last();
        firstRow.nextUntil(lastRow).addClass("selected");
    }
}
function hubComputerGroupChanged(e) {
    var request = {
        "Type": "SetComputerGroup",
        "ComputerName": $(e.currentTarget).parent().parent().find("td[prop='ComputerName']").html(),
        "ComputerGroup": $(e.currentTarget).val(),
        "AuthenticationToken": InstaTech.AuthenticationToken
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
function remoteControlComputerHub() {
    if ($("#tableComputerHub tbody tr.selected").length > 1) {
        showDialog("Too Many Selections", "You can only have one computer selected when starting a remote control session.");
        return;
    }
    if ($("#tableComputerHub tbody tr.selected").length == 0) {
        showDialog("Selection Required", "You must select a computer first.");
        return;
    }
    window.open(window.location.origin + "/Remote_Control/?Computer=" + $("#tableComputerHub tbody tr.selected td[prop='ComputerName'").text() + "&UserID=" + InstaTech.UserID + "&AuthenticationToken=" + InstaTech.AuthenticationToken, "_blank")
}
function populateHubComputerGroups(e) {
    if (typeof InstaTech.ComputerGroups == "undefined") {
        window.setTimeout(function () {
            populateHubComputerGroups();
        }, 200)
        return;
    }
    if (InstaTech.ComputerGroups.length > $("#selectComputerHubGroups").children().length)
    {
        $("#selectComputerHubGroups").html("<option value=''>Any</option>");
        for (var i = 0; i < InstaTech.ComputerGroups.length; i++) {
            var option = document.createElement("option");
            option.value = InstaTech.ComputerGroups[i];
            option.innerHTML = InstaTech.ComputerGroups[i];
            $("#selectComputerHubGroups").append(option);
        }
    }
}

function inputDeployDragOver (e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = "copy";
};
function inputDeployDrop (e) {
    e.preventDefault();
    if (e.dataTransfer.files.length < 1) {
        return;
    }
    if (e.dataTransfer.files.length > 1) {
        showDialog("File Limit Exceeded", "You can only deploy one file at a time.");
        return;
    }
    $("#inputDeployFile").hide();
    $("#inputDeployFile2").show();
    $("#inputDeployFile2").val(e.dataTransfer.files[0].name);
    InstaTech.Temp.fileDeployList = e.dataTransfer.files;
};
function openDeployerTool() {
    if ($("#tableComputerHub tbody tr.selected").length == 0) {
        showDialog("Selection Required", "You must select at least one computer first.");
        return;
    }
    if ($("#divDeployFile").is(":visible")) {
        $("#divDeployFile").slideUp();
    }
    else
    {
        $("#divDeployFile").slideDown(function () {
            window.scroll(null, $("#divDeployFile").offset().top);
        });
    }
}

function deployFiles() {
    if ($("#tableComputerHub tbody tr.selected").length == 0) {
        showDialog("Selection Required", "You must select at least one computer first.");
        return;
    }
    var fileList;
    if (typeof InstaTech.Temp.fileDeployList != "undefined" && InstaTech.Temp.fileDeployList.length > 0) {
        fileList = InstaTech.Temp.fileDeployList;
    }
    else if ($("#inputDeployFile")[0].files.length > 0) {
        fileList = $("#inputDeployFile")[0].files;
    }
    else {
        showDialog("File Required", "You must select a file to deploy.");
        return;
    }
    if (fileList.length > 1) {
        showDialog("File Limit Exceeded", "You can only deploy one file at a time.");
        return;
    }
    var extDot = fileList[0].name.lastIndexOf(".");
    var ext = fileList[0].name.substring(extDot).toLowerCase();
    if (ext != ".ps1" && ext != ".bat" && ext != ".exe") {
        showDialog("Unsupported Format", "File format is unsupported.");
        return;
    }
    var file = fileList[0];
    var strPath = "/Services/File_Transfer.cshtml";
    var fd = new FormData();
    fd.append('fileUpload', file);
    var targetElements = $("#tableComputerHub tbody tr.selected td[prop='ComputerName'");
    var xhr = new XMLHttpRequest();
    xhr.open('POST', strPath, true);
    xhr.onload = function () {
        if (xhr.status === 200) {
            var fileName = xhr.responseText;
            var url = location.origin + "/Services/File_Transfer.cshtml?file=" + fileName;
            targetElements.each(function (index, elem) {
                var request = {
                    "Type": "FileDeploy",
                    "FileName": fileName,
                    "URL": url,
                    "Arguments": $("#inputDeployArguments").val().trim(),
                    "TargetComputer": elem.innerHTML,
                    "FromID": InstaTech.UserID,
                    "AuthenticationToken": InstaTech.AuthenticationToken
                };
                InstaTech.Socket_Main.send(JSON.stringify(request));
            })
            showTooltip($("#textDeployResults"), "top", "green", "File uploaded successfully.  Results will be displayed after processing has finished.");
        }
        else {
            showDialog("Upload Failed", "File upload failed.");
        }
    };
    xhr.send(fd);
}
function convertResultsToJSON() {
    $("#textDeployResults").val(JSON.stringify(InstaTech.Temp.DeployResults));
}
function clearDeployResults() {
    $("#textDeployResults").val("");
    delete InstaTech.Temp.DeployResults;
}
function openHubConsole() {
    if ($("#tableComputerHub tbody tr.selected").length == 0) {
        showDialog("Selection Required", "You must select at least one computer first.");
        return;
    }
    if ($("#divHubConsole").is(":visible")) {
        $("#divHubConsole").slideUp();
    }
    else {
        $("#divHubConsole").slideDown(function () {
            window.scroll(null, $("#divHubConsole").offset().top);
        });
    }
}
function inputConsoleKeyPress(e) {
    if (e.key.toLowerCase() == "enter") {
        hubConsoleSubmit();
    }
}
function inputConsoleKeyDown(e) {
    if (e.key.toLowerCase() == "up" || e.key.toLowerCase() == "arrowup") {
        if (InstaTech.Temp.ConsoleHistoryPosition != undefined) {
            if (InstaTech.Temp.ConsoleHistory[InstaTech.Temp.ConsoleHistoryPosition - 1] != undefined) {
                $("#inputHubConsole").val(InstaTech.Temp.ConsoleHistory[InstaTech.Temp.ConsoleHistoryPosition - 1]);
                InstaTech.Temp.ConsoleHistoryPosition--;
            }
        }
    }
    else if (e.key.toLowerCase() == "down" || e.key.toLowerCase() == "arrowdown") {
        if (InstaTech.Temp.ConsoleHistoryPosition != undefined) {
            if (InstaTech.Temp.ConsoleHistory[InstaTech.Temp.ConsoleHistoryPosition + 1] != undefined) {
                $("#inputHubConsole").val(InstaTech.Temp.ConsoleHistory[InstaTech.Temp.ConsoleHistoryPosition + 1]);
                InstaTech.Temp.ConsoleHistoryPosition++;
            }
            else
            {
                if (InstaTech.Temp.ConsoleHistoryPosition == InstaTech.Temp.ConsoleHistory.length - 1) {
                    $("#inputHubConsole").val("");
                    InstaTech.Temp.ConsoleHistoryPosition++;
                }
            }
        }
    }
    else if (e.key.toLowerCase() == "esc" || e.key.toLowerCase() == "escape") {
        $("#inputHubConsole").val("");
        InstaTech.Temp.ConsoleHistoryPosition = InstaTech.Temp.ConsoleHistory.length;
    }
}
function hubConsoleSubmit(e) {
    if ($("#tableComputerHub tbody tr.selected").length == 0) {
        showDialog("Selection Required", "You must select at least one computer first.");
        return;
    }
    if ($("#inputHubConsole").val().trim() == "") {
        return;
    }
    var command = $("#inputHubConsole").val();
    $("#inputHubConsole").val("");
    InstaTech.Temp.ConsoleHistory = InstaTech.Temp.ConsoleHistory || [];
    InstaTech.Temp.ConsoleHistory.push(command);
    InstaTech.Temp.ConsoleHistoryPosition = InstaTech.Temp.ConsoleHistory.length;
    var language;
    if ($("#radioConsoleModePS").is(":checked")) {
        language = "PowerShell";
    }
    else if ($("#radioConsoleModeBAT").is(":checked")) {
        language = "Batch";
    }
    else {
        showDialog("Script Language Required", "You must select a scripting language.");
        return;
    }
    var request = {
        "Type": "ConsoleCommand",
        "Language": language,
        "Command": btoa(command)
    }
    var targetElements = $("#tableComputerHub tbody tr.selected td[prop='ComputerName'");
    targetElements.each(function (index, elem) {
        var request = {
            "Type": "ConsoleCommand",
            "Language": language,
            "Command": btoa(command),
            "TargetComputer": elem.innerHTML,
            "FromID": InstaTech.UserID,
            "AuthenticationToken": InstaTech.AuthenticationToken
        };
        InstaTech.Socket_Main.send(JSON.stringify(request));
    })
}