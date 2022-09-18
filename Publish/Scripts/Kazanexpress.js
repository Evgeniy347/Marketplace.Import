
function Authorization() {
      
    var login = "markinevgeniy2010@mail.ru";
    var password = "s";

    var loginInput = document.querySelector("#fm-login-id");
    var pwdInput = document.querySelector("#fm-login-password");
 
    if (!loginInput || !pwdInput) {

        var checkVerifyLog = document.querySelector("#baxia-dialog-content");
        if (checkVerifyLog) {
            console.error("find baxia-dialog-content");
            console.error("Application:Exit");
            return;
        };

        var buttonOK = document.querySelector(".comet-btn");
        if (buttonOK) {
            if (!buttonOK.AuthorizationClick)
                buttonOK.click();
            buttonOK.AuthorizationClick = true;
            setTimeout(Authorization, 10);
            return;
        }
    }

    var buttonOK = document.querySelector(".login-submit");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password);

    buttonOK.click(); 
}


function SetValueInput() {


}

Authorization(); 