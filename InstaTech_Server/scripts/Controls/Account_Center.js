function initAccountCenter() {
    getTechAccounts();
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
    InstaTech.Temp.rowRestore = row.html();
    row.addClass("editing");
    row.children().each(function (index, elem) {
        var readonly = "";
        var prop = elem.getAttribute("prop");
        if (prop == "UserID")
        {
            readonly = "readonly disabled";
        }
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
        else
        {
            elem.innerHTML = '<input type="text" value="' + elem.innerHTML + '" prop="' + prop + '" onkeydown="if (event.key.toLowerCase() == \'enter\') { saveTechAccount(); }"' + readonly + ' />'
        }
    })
}
function newTechAccount() {

}
function deleteTechAccount() {

}
function saveTechAccount(editRow) {
    var account = InstaTech.Tech_Accounts.filter(function (value, index) {
        return value.UserID == $(editRow).find("input[prop='UserID']").val();
    })[0];
    account.FirstName = $(editRow).find("input[prop='FirstName']").val();
    account.LastName = $(editRow).find("input[prop='LastName']").val();
    account.Email = $(editRow).find("input[prop='Email']").val();
    account.TempPassword = $(editRow).find("input[prop='TempPassword']").val();
    account.LastBadLogin = new Date($(editRow).find("input[prop='LastBadLogin']").val());
    account.BadLoginAttempts = $(editRow).find("input[prop='BadLoginAttempts']").val();
    account.ComputerGroups = $(editRow).find("input[prop='ComputerGroups']").val().split("\n");
    account.AccessLevel = $(editRow).find("select[prop='AccessLevel']").val().replace("Standard", 0).replace("Admin", 1);
    var request = {
        "Type": "SaveTechAccount",
        "Account": account
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
}
function populateAccountTable(e) {
    var tr = document.createElement("tr");
    tr.onclick = function (e) {
        $("#tableAccountCenter tbody tr").removeClass("selected");
        e.currentTarget.classList.add("selected");
        if (e.currentTarget.classList.contains("editing")) {
            return;
        }
        var editRow = $("#tableAccountCenter tbody tr.editing");
        if (editRow.length > 0) {
            saveTechAccount(editRow);
        }
    }
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
    InstaTech.Tech_Accounts[e].ComputerGroups.forEach(function (value, index) {
        td.innerHTML += value + "\n"
    })
    td.setAttribute("prop", "ComputerGroups");
    td.innerHTML = td.innerHTML.trim();
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