function handleGetConfiguration(e) {
    if (e.Status == "unauthorized") {
        showDialog("Unauthorized", "You are not authorized to view the configuration.");
        return;
    }
    else if (e.Status == "ok") {
        var config = e.Config;
        $("#inputCompanyName").val(config.Company_Name);
        $("#inputLicenseKey").val(config.License_Key);
        $("#inputDefaultAdmin").val(config.Default_Admin);
        $("#toggleDemoMode").attr("on", config.Demo_Mode);
        $("#toggleFileEncryption").attr("on", config.File_Encryption);
        $("#toggleADEnabled").attr("on", config.Active_Directory_Enabled);
        $("#inputADTechGroup").val(config.Active_Directory_Tech_Group);
        $("#toggleFeatureChat").attr("on", config.Feature_Enabled_Chat);
        $("#toggleFeatureRemoteControl").attr("on", config.Feature_Enabled_Remote_Control);
        $("#toggleFeatureAccountCenter").attr("on", config.Feature_Enabled_Account_Center);
        $("#toggleFeatureComputerHub").attr("on", config.Feature_Enabled_Computer_Hub);
        $("#toggleFeatureConfiguration").attr("on", config.Feature_Enabled_Configuration);
        $("#inputEmailServer").val(config.Email_SMTP_Server);
        $("#inputEmailPort").val(config.Email_SMTP_Port);
        $("#inputEmailUsername").val(config.Email_SMTP_Username);
        $("#inputEmailPassword").val(config.Email_SMTP_Password);
        $("#selectDefaultRCDownload").val(config.Default_RC_Download);
        for (var i = 0; i < config.Support_Categories.length; i++) {
            if ($("#selectSupportQueues").find("option[value='" + config.Support_Categories[i].Queue + "']").length == 0) {
                var option = document.createElement("option");
                option.value = config.Support_Categories[i].Queue;
                option.innerHTML = config.Support_Categories[i].Queue;
                $("#selectSupportQueues").append(option);
            }
        }
        $("#selectSupportQueues")[0].selectedIndex = -1;
        for (var i = 0; i < config.Computer_Groups.length; i++) {
            var option = document.createElement("option");
            option.value = config.Computer_Groups[i];
            option.innerHTML = config.Computer_Groups[i];
            $("#selectComputerGroups").append(option);
        }
    }
}
function handleSetSupportQueue(e) {
    if (e.Status == "failed") {
        showDialog("Change Failed", "Failed to change queue.  Please refresh the webpage and try again.");
        return;
    }
    if (e.Status == "ok") {
        showTooltip($("#selectSupportQueues"), "left", "green", "Saved.");
        return;
    }
}

function handleAddSupportCategory(e) {
    if (e.Status == "length") {
        showDialog("Invalid Category", "The category name must be at least 3 characters long.");
        return;
    }
    if (e.Status == "exists") {
        showDialog("Category Already Exists", "The category already exists.  Please try another, or add a new type to this category.");
        return;
    }
    if (e.Status == "ok") {
        showTooltip($("#selectSupportCategories"), "left", "green", "Category added.");
        var request = {
            "Type": "GetSupportCategories",
            "ElementID": "selectSupportCategories"
        };
        InstaTech.Socket_Main.send(JSON.stringify(request));
        return;
    }
}
function handleDeleteSupportCategory(e) {
    if (e.Status == "ok") {
        showTooltip($("#selectSupportCategories"), "left", "green", "Category deleted.");
        var request = {
            "Type": "GetSupportCategories",
            "ElementID": "selectSupportCategories"
        };
        InstaTech.Socket_Main.send(JSON.stringify(request));
    }
}

function handleAddSupportType(e) {
    if (e.Status == "length") {
        showDialog("Invalid Type", "The type name must be at least 3 characters long.");
        return;
    }
    if (e.Status == "exists") {
        showDialog("Type Already Exists", "The type already exists in this category.  Please try another.");
        return;
    }
    if (e.Status == "ok") {
        showTooltip($("#selectSupportTypes"), "left", "green", "Category added.");
        var request = {
            "Type": "GetSupportTypes",
            "SupportCategory": $("#selectSupportCategories").val(),
            "ElementID": "selectSupportTypes"
        };
        InstaTech.Socket_Main.send(JSON.stringify(request));
        return;
    }
}

function handleDeleteSupportType(e) {
    if (e.Status == "ok") {
        showTooltip($("#selectSupportTypes"), "left", "green", "Type deleted.");
        $("#selectSupportQueues")[0].selectedIndex = -1;
        var request = {
            "Type": "GetSupportTypes",
            "SupportCategory": $("#selectSupportCategories").val(),
            "ElementID": "selectSupportTypes"
        };
        InstaTech.Socket_Main.send(JSON.stringify(request));
    }
}

function handleAddSupportQueue(e) {
    if (e.Status == "failed") {
        showDialog("Add Failed", "Failed to add queue.  Please refresh the webpage and try again.");
        return;
    }
    if (e.Status == "ok") {
        showTooltip($("#selectSupportQueues"), "left", "green", "Saved.");
        var option = document.createElement("option");
        option.innerHTML = e.SupportQueue;
        option.value = e.SupportQueue;
        $("#selectSupportQueues").append(option);
        $("#selectSupportQueues").val(option.value);
        return;
    }
}
function handleAddComputerGroup(e) {
    if (e.Status == "exists") {
        showDialog("Already Exists", "The computer group already exists.");
        return;
    }
    if (e.Status == "ok") {
        var option = document.createElement("option");
        option.value = e.Group;
        option.innerHTML = e.Group;
        $("#selectComputerGroups").append(option);
        showTooltip($("#selectComputerGroups"), "left", "green", "Group added.");
        return;
    }
}
function handleDeleteComputerGroup(e) {
    if (e.Status == "ok") {
        $("#selectComputerGroups").children().filter(function(index, elem) {
            return elem.value == e.Group;
        }).remove();
        showTooltip($("#selectComputerGroups"), "left", "green", "Group deleted.");
    }
}