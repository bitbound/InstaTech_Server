function handleGetTechAccounts(e) {
    if (e.Status == "unauthorized") {
        showDialog("Unauthorized", "You are not authorized to view tech accounts.");
        return;
    }
    else if (e.Status == "ok") {
        InstaTech.Tech_Accounts = e.TechAccounts;
        populateAccountTable(0);
    }
}

function handleSaveTechAccount(e) {
    if (e.Status == "notfound") {
        var editRow = $("#divAccountCenter .editing");
        $("#divAccountCenter .editing").html(InstaTech.Temp.rowRestore);
        delete InstaTech.Temp.rowRestore;
        showTooltip(editRow, "center", "red", "Account not found.  Please refresh the page.");
        return;
    }
    else if (e.Status == "failed") {
        var editRow = $("#divAccountCenter .editing");
        $("#divAccountCenter .editing").html(InstaTech.Temp.rowRestore);
        delete InstaTech.Temp.rowRestore;
        showTooltip(editRow, "center", "red", "Failed to save account.");
        $("#accountSaveOK").hide();
        $("#accountSaveCancel").hide();
        return;
    }
    else if (e.Status == "ok") {
        $("#divAccountCenter .editing").children().each(function (index, elem) {
            elem.innerHTML = $(elem.children[0]).val();
        })
        delete InstaTech.Temp.rowRestore;
        showTooltip($("#divAccountCenter .editing"), "center", "green", "Account saved.");
        $("#divAccountCenter .editing").removeClass("editing");
        $("#accountSaveOK").hide();
        $("#accountSaveCancel").hide();
        return;
    }
}
function handleNewTechAccount(e) {
    if (e.Status == "exists") {
        var editRow = $("#divAccountCenter .editing");
        showTooltip(editRow, "center", "red", "User ID already exists.  Please use another.");
        return;
    }
    else if (e.Status == "length") {
        var editRow = $("#divAccountCenter .editing");
        showTooltip(editRow, "center", "red", "User ID must be at least 3 characters in length.");
        return;
    }
    else if (e.Status == "invalid") {
        var editRow = $("#divAccountCenter .editing");
        showTooltip(editRow, "center", "red", "Invalid User ID.  Please use only alphanumeric characters.");
        return;
    }
    else if (e.Status == "failed") {
        var editRow = $("#divAccountCenter .editing");
        showTooltip(editRow, "center", "red", "Failed to create new account.");
        editRow.remove();
        $("#accountSaveOK").hide();
        $("#accountSaveCancel").hide();
        return;
    }
    else if (e.Status == "ok") {
        $("#divAccountCenter .editing").children().each(function (index, elem) {
            elem.innerHTML = $(elem.children[0]).val();
        })
        InstaTech.Tech_Accounts.push(e.Account);
        delete InstaTech.Temp.rowRestore;
        showTooltip($("#divAccountCenter .editing"), "center", "green", "Account created.");
        $("#divAccountCenter .editing").removeClass("editing");
        $("#divAccountCenter .new-account").removeClass("new-account");
        $("#accountSaveOK").hide();
        $("#accountSaveCancel").hide();
        return;
    }
}
function handleDeleteTechAccount(e) {
    var row = $("#tableAccountCenter tbody tr.selected");
    if (e.Status == "notfound") {
        showTooltip(row, "center", "red", "Account not found.  Please refresh the page.");
        return;
    }
    else if (e.Status == "failed") {
        showTooltip(row, "center", "red", "Failed to delete account.");
        return;
    }
    else if (e.Status == "ok") {
        showTooltip(row, "center", "green", "Account deleted.");
        row.remove();
        return;
    }
}