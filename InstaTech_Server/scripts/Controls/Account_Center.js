function initAccountCenter() {
    getTechAccounts();
    getAllComputerGroups();
}
function getTechAccounts() {
    var request = {
        "Type": "GetTechAccounts",
        "AuthenticationToken": InstaTech.AuthenticationToken
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
}

function refreshAccountCenter() {
    $("#tableAccountCenter tbody tr").remove();
    getTechAccounts();
}
function searchTechs() {
    window.setTimeout(function () {
        var searchString = $("#inputSearchTechs").val().toLowerCase();
        $("#tableAccountCenter tbody tr").each(function (index, elem) {
            var match = false;
            var cells = $(elem).children();
            for (var i = 0; i < cells.length; i++) {
                if (cells[i].innerHTML.toLowerCase().search(searchString) > -1) {
                    match = true;
                    break
                }
            }
            if (match) {
                $(elem).show();
            }
            else {
                $(elem).hide();
            }
        })
        window.scroll(0, $("#tableAccountCenter").offset().top);
    }, 50)
}
function editTechAccount() {
    var row = $("#tableAccountCenter tbody tr.selected");
    if (row.length == 0)
    {
        return;
    }
    if ($("#tableAccountCenter .editing").length > 0) {
        return;
    }
    InstaTech.Temp.rowRestore = row.html();
    row.addClass("editing");
    row.children().each(function (index, elem) {
        var prop = elem.getAttribute("prop");
        if (prop == "AccessLevel")
        {
            var standard = "";
            var admin = "";
            if (elem.innerHTML == "Standard")
            {
                standard = "selected";
            }
            else if (elem.innerHTML == "Admin")
            {
                admin = "selected";
            }
            elem.innerHTML = '<select prop="' + prop + '">\
                    <option ' + standard + ' value="Standard">Standard</option>\
                    <option ' + admin + ' value="Admin">Admin</option>\
                </select>';
        }
        else if (prop == "ComputerGroups")
        {
            var select = document.createElement("select");
            select.multiple = true;
            select.setAttribute("prop", prop);
            for (var i = 0; i < InstaTech.ComputerGroups.length; i++)
            {
                var option = document.createElement("option");
                option.value = InstaTech.ComputerGroups[i];
                option.innerHTML = InstaTech.ComputerGroups[i];
                if (elem.innerHTML.search(InstaTech.ComputerGroups[i]) > -1)
                {
                    option.setAttribute("selected", true);
                }
                select.options.add(option);
            }
            elem.innerHTML = select.outerHTML;
        }
        else
        {
            var input = document.createElement("input");
            input.type = "text";
            input.setAttribute("prop", prop);
            if (prop == "UserID")
            {
                input.setAttribute("readonly", true);
                input.setAttribute("disabled", true);
            }
            input.defaultValue = elem.innerHTML.toString();
            elem.innerHTML = input.outerHTML;
        }
    })
    $("#tableAccountCenter input:enabled, #tableAccountCenter select").on("keydown", function (e) {
        inputAccountKeyDown(e);
    });
    $("#tableAccountCenter").find("input:enabled").first().focus();
    $("#accountSaveOK").show();
    $("#accountSaveCancel").show();
}
function newTechAccount() {
    if ($("#tableAccountCenter .editing").length > 0)
    {
        return;
    }
    var tr = document.createElement("tr");
    tr.classList.add("editing");
    tr.classList.add("new-account");
    var td = document.createElement("td");
    td.setAttribute("prop", "UserID");
    td.setAttribute("required", true);
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "FirstName");
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "LastName");
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "Email");
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "TempPassword");
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "LastBadLogin");
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "BadLoginAttempts");
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "ComputerGroups");
    tr.appendChild(td);
    td = document.createElement("td");
    td.setAttribute("prop", "AccessLevel");
    tr.appendChild(td);
    for (var i = 0; i < tr.children.length; i++)
    {
        var elem = tr.children[i];
        var prop = elem.getAttribute("prop");
        if (prop == "AccessLevel") {
            var standard = "";
            var admin = "";
            elem.innerHTML = '<select prop="' + prop + '">\
                    <option value="Standard">Standard</option>\
                    <option value="Admin">Admin</option>\
                </select>';
        }
        else if (prop == "ComputerGroups") {
            var select = document.createElement("select");
            select.multiple = true;
            select.setAttribute("prop", prop);
            for (var i2 = 0; i2 < InstaTech.ComputerGroups.length; i2++) {
                var option = document.createElement("option");
                option.value = InstaTech.ComputerGroups[i2];
                option.innerHTML = InstaTech.ComputerGroups[i2];
                select.options.add(option);
            }
            elem.innerHTML = select.outerHTML;
        }
        else {
            var input = document.createElement("input");
            input.type = "text";
            input.setAttribute("prop", prop);
            elem.innerHTML = input.outerHTML;
        }
    }

    $("#tableAccountCenter tbody").prepend(tr);
    $("#tableAccountCenter input:enabled, #tableAccountCenter select").on("keydown", function (e) {
        inputAccountKeyDown(e);
    });
    $("#tableAccountCenter").find("input:enabled").first().focus();
    tr.onclick = accountRowClicked;
    $("#accountSaveOK").show();
    $("#accountSaveCancel").show();
}
function deleteTechAccount() {
    var row = $("#tableAccountCenter tbody tr.selected");
    if (row.length == 0) {
        return;
    }
    if ($("#tableAccountCenter .editing").length > 0) {
        return;
    }
    var id = $(row).find("td[prop='UserID']").html();
    var dialog = document.createElement("div");
    dialog.innerHTML = "Are you sure you want to delete this account?<br/><br/>Account Name: <strong>" + id + "</strong>";
    $(dialog).dialog({
        width: document.body.clientWidth * .5,
        title: "Confirm Deletion",
        classes: { "ui-dialog-title": "center-aligned" },
        buttons: [
            {
                text: "OK",
                click: function () {
                    var request = {
                        "Type": "DeleteTechAccount",
                        "UserID": id,
                        "AuthenticationToken": InstaTech.AuthenticationToken
                    }
                    InstaTech.Socket_Main.send(JSON.stringify(request));
                    $(this).dialog("close");
                }
            },
            {
                text: "Cancel",
                click: function () {
                    $(this).dialog("close");
                }
            }
        ],
        close: function () {
            $(this).dialog('destroy').remove();
        }
    });
}
function accountRowClicked(e) {
    $("#tableAccountCenter tbody tr").removeClass("selected");
    e.currentTarget.classList.add("selected");
    if (e.currentTarget.classList.contains("editing")) {
        return;
    }
    var editRow = $("#tableAccountCenter tbody tr.editing");
    if (editRow.length > 0) {
        saveTechAccount();
    }
}
function inputAccountKeyDown(e) {
    if (e.key.toLowerCase() == 'enter') {
        saveTechAccount();
    }
    else if (e.key.toLowerCase() == "escape")
    {
        cancelEditing();
    }
}
function cancelEditing() {
    var editRow = $("#divAccountCenter .editing");
    if (editRow.hasClass("new-account")) {
        showTooltip(editRow, "center", "red", "Account creation canceled.");
        $("#divAccountCenter .editing").remove();
    }
    else {
        $("#divAccountCenter .editing").html(InstaTech.Temp.rowRestore);
        delete InstaTech.Temp.rowRestore;
        $("#divAccountCenter .editing").removeClass("editing");
        showTooltip(editRow, "center", "red", "Edit canceled.");
    }
    $("#accountSaveOK").hide();
    $("#accountSaveCancel").hide();
}
function saveTechAccount() {
    var account;
    var editRow = $("#tableAccountCenter tbody tr.editing");
    if (editRow.hasClass("new-account"))
    {
        account = new Tech_Account();
        account.UserID = $(editRow).find("input[prop='UserID']").val();
    }
    else
    {
        account = InstaTech.Tech_Accounts.filter(function (value, index) {
            return value.UserID == $(editRow).find("input[prop='UserID']").val();
        })[0];
    }
    account.FirstName = $(editRow).find("input[prop='FirstName']").val();
    account.LastName = $(editRow).find("input[prop='LastName']").val();
    account.Email = $(editRow).find("input[prop='Email']").val();
    account.TempPassword = $(editRow).find("input[prop='TempPassword']").val();
    account.LastBadLogin = new Date($(editRow).find("input[prop='LastBadLogin']").val() || "1/1/01");
    account.BadLoginAttempts = $(editRow).find("input[prop='BadLoginAttempts']").val() || 0;
    account.ComputerGroups = $(editRow).find("select[prop='ComputerGroups']").val();
    account.AccessLevel = $(editRow).find("select[prop='AccessLevel']").val().replace("Standard", 0).replace("Admin", 1);

    if (editRow.hasClass("new-account")) {
        var request = {
            "Type": "NewTechAccount",
            "Account": account,
            "AuthenticationToken": InstaTech.AuthenticationToken
        }
    }
    else {
        var request = {
            "Type": "SaveTechAccount",
            "Account": account,
            "AuthenticationToken": InstaTech.AuthenticationToken
        }
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
}

function populateAccountTable(e) {
    var tr = document.createElement("tr");
    tr.onclick = accountRowClicked;
    var td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].UserID;
    td.setAttribute("prop", "UserID");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].FirstName;
    td.setAttribute("prop", "FirstName");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].LastName;
    td.setAttribute("prop", "LastName");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].Email;
    td.setAttribute("prop", "Email");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].TempPassword;
    td.setAttribute("prop", "TempPassword");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = parseNETDate(InstaTech.Tech_Accounts[e].LastBadLogin).toLocaleString();
    td.setAttribute("prop", "LastBadLogin");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].BadLoginAttempts;
    td.setAttribute("prop", "BadLoginAttempts");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].ComputerGroups.toString();
    td.setAttribute("prop", "ComputerGroups");
    tr.appendChild(td);
    td = document.createElement("td");
    td.innerHTML = InstaTech.Tech_Accounts[e].AccessLevel.toString().replace(1, "Admin").replace(0, "Standard");
    td.setAttribute("prop", "AccessLevel");
    tr.appendChild(td);
    $("#tableAccountCenter tbody").append(tr);
    if (InstaTech.Tech_Accounts[e + 1] != null) {
        window.setTimeout(function () { populateAccountTable(e + 1) }, 1);
    }
}