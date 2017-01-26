function handleSessionEnded(e) {
    if (InstaTech.Socket_Main.readyState == 1) {
        window.setTimeout(function () {
            handleSessionEnded(e);
        }, 500);
        return;
    }
    var message = "Your session has ended.";
    if (e.Details) {
        message += "<br/><br/>" + e.Details;
    }
    showDialog("Session Ended", message);
}

function handleTechMainLogin(e) {
    if (e.Status == "new required") {
        $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").attr("required", true);
        $("#inputTechMainConfirmNewPassword, #inputTechMainNewPassword").parent("td").parent("tr").show();
        return;
    }
    else if (e.Status == "ok") {
        clearCachedCreds();
        InstaTech.Context = "Technician";
        InstaTech.UserID = $("#inputTechMainUserID").val();
        InstaTech.AuthenticationToken = e.AuthenticationToken;
        if (document.getElementById("inputTechMainRememberMe").checked) {
            localStorage["RememberMe"] = true;
            localStorage["UserID"] = InstaTech.UserID;
            localStorage["AuthenticationToken"] = InstaTech.AuthenticationToken;
        }
        $("#divMainTechLoginForm").slideUp();
        setMainLoginFrame();
    }
    else if (e.Status == "invalid") {
        clearCachedCreds();
        showDialog("Incorrect Credentials", "The user ID or password is incorrect.  Please try again.");
        return;
    }
    else if (e.Status == "locked") {
        showDialog("Account Locked", "Your account as been locked due to failed login attempts.  It will unlock automatically after 10 minutes.  Please try again later.");
        return;
    }
    else if (e.Status == "temp ban") {
        showDialog("Temporary Ban", "Due to failed login attempts, you must refresh your browser to try again.");
        return;
    }
    else if (e.Status == "password mismatch") {
        showDialog("Password Mismatch", "The passwords you entered don't match.  Please retype them.");
        return;
    }
    else if (e.Status == "password length") {
        showDialog("Password Length", "Your new password must be between 8 and 20 characters long.");
        return;
    }
}
function handleForgotPassword(e) {
    if (e.Status == "invalid") {
        showDialog("Invalid User ID", "The user ID couldn't be found.");
    }
    else if (e.Status == "noemail") {
        showDialog("No Email", "There is no email address on file for this account.  Please contact your system administrator.");
    }
    else if (e.Status == "error") {
        showDialog("Error Sending Email", "There was an error sending the email.  Please contact your system administrator.");
    }
    else if (e.Status == "ok") {
        showDialog("Password Reset Successful", "A temporary password has been sent to your email.  Please check your inbox.");
    }
}