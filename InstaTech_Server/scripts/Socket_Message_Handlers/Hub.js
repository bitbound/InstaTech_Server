function handleSearchComputerHub(e) {
    if (e.Status == "denied") {
        showDialog("Access Denied", "You don't have access to computers in that group.");
        return;
    }
    if (e.Status == "ok") {
        InstaTech.Hub_Computers = e.Computers;
        if (InstaTech.Hub_Computers.length == 0) {
            showDialog("No Results", "No computers matched the search criteria.");
        }
        else {
            $("#tableComputerHub tbody").html("");
            for (var i = 0; i < InstaTech.Hub_Computers.length; i++) {
                var tr = document.createElement("tr");
                tr.onclick = hubRowClicked;
                var td = document.createElement("td");
                td.innerHTML = InstaTech.Hub_Computers[i].ComputerName;
                td.setAttribute("prop", "ComputerName");
                tr.appendChild(td);
                td = document.createElement("td");
                td.innerHTML = parseNETDate(InstaTech.Hub_Computers[i].LastReboot).toLocaleString();
                td.setAttribute("prop", "LastReboot");
                tr.appendChild(td);
                td = document.createElement("td");
                td.innerHTML = InstaTech.Hub_Computers[i].CurrentUser;
                td.setAttribute("prop", "CurrentUser");
                tr.appendChild(td);
                td = document.createElement("td");
                td.innerHTML = InstaTech.Hub_Computers[i].LastLoggedOnUser;
                td.setAttribute("prop", "LastLoggedOnUser");
                tr.appendChild(td);
                td = document.createElement("td");
                var sel = document.createElement("select");
                sel.onchange = hubComputerGroupChanged;
                var option = document.createElement("option");
                sel.options.add(option);
                for (var i2 = 0; i2 < InstaTech.ComputerGroups.length; i2++) {
                    var option = document.createElement("option");
                    option.value = InstaTech.ComputerGroups[i2];
                    option.innerHTML = InstaTech.ComputerGroups[i2];
                    if (InstaTech.Hub_Computers[i].ComputerGroup == InstaTech.ComputerGroups[i2]) {
                        option.selected = true;
                    }
                    sel.options.add(option);
                }
                td.appendChild(sel);
                td.setAttribute("prop", "ComputerGroup");
                tr.appendChild(td);
                $("#tableComputerHub tbody").append(tr);
            }
        }
        window.scroll(0, $("#tableComputerHub").offset().top);
    }
}
function handleSetComputerGroup(e) {
    if (e.Status == "denied") {
        showDialog("Access Denied", "You do not have access to modify that group.");
        return;
    }
    if (e.Status == "unknown") {
        showDialog("Computer Not Found", "Unable to find that computer.  Please refresh your search results.");
        return;
    }
    if (e.Status == "ok") {
        showTooltip($("select:focus"), "left", "green", "Group changed.");
    }
}
function handleFileDeploy(e) {
    if (e.Status == "denied") {
        $("#textDeployResults")[0].value += "[FAILED]\nAccess to " + e.TargetComputer + " denied.";
    }
    else if (e.Status == "notfound") {
        $("#textDeployResults")[0].value += "[FAILED]\nComputer " + e.TargetComputer + " wasn't found.";
    }
    else if (e.Status == "ok") {
        InstaTech.Temp.DeployResults = InstaTech.Temp.DeployResults || [];
        InstaTech.Temp.DeployResults.push(e);
        $("#textDeployResults")[0].value += "[SUCCESS]\nComputer: " + e.TargetComputer + "\nExit Code: " + e.ExitCode
            + "\nOutput: " + e.Output + "\Errors: " + e.Error;
    }
    $("#textDeployResults")[0].value += "\n-------------------------\n\n";
    $("#textDeployResults")[0].scrollTop = $("#textDeployResults")[0].scrollHeight;
}
function handleConsoleCommand(e) {
    if (e.Status == "denied") {
        $("#textConsoleOutput")[0].value += "[" + e.TargetComputer  + "]\nAccess to denied.\n";
    }
    else if (e.Status == "notfound") {
        $("#textConsoleOutput")[0].value += "[" + e.TargetComputer + "]\nComputer wasn't found.\n";
    }
    else if (e.Status == "ok") {
        $("#textConsoleOutput")[0].value += "[" + e.TargetComputer + "]: " + e.Output + "\n";
    }
    $("#textConsoleOutput")[0].scrollTop = $("#textConsoleOutput")[0].scrollHeight;
}
function handleNewConsole(e) {
    if (e.Status == "denied") {
        $("#textConsoleOutput")[0].value += "[" + e.TargetComputer + "]\nAccess to denied.\n";
    }
    else if (e.Status == "notfound") {
        $("#textConsoleOutput")[0].value += "[" + e.TargetComputer + "]\nComputer wasn't found.\n";
    }
    else if (e.Status == "ok") {
        $("#textConsoleOutput")[0].value += "[" + e.TargetComputer + "]: New console session started.\n";
    }
    $("#textConsoleOutput")[0].scrollTop = $("#textConsoleOutput")[0].scrollHeight;
}