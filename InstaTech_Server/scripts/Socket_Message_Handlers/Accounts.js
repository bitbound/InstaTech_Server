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
    // TODO.  Delete InstaTech.Temp.rowRestore when done.
    if (e.Status == "notfound") {
        return;
    }
    else if (e.Status == "failed") {
        return;
    }
    else if (e.Status == "ok") {
        return;
    }
}