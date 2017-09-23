function getAllComputerGroups() {
    var request = {
        "Type": "GetAllComputerGroups",
        "AuthenticationToken": InstaTech.AuthenticationToken
    }
    InstaTech.Socket_Main.send(JSON.stringify(request));
}