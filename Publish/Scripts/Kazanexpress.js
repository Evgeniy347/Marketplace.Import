
function MPS_Init() {

    if (!window.MPS_Context)
        return;

    setTimeout(function () {
        if (!window.document.body.StartAuthorization) {
            window.document.body.StartAuthorization = true;
            MPS_PushLog("StartAuthorizationKazanexpress");
            MPS_GetToken();
        }
    }, 1000);
}

function MPS_GetToken() {

    var login = "{Login}";
    var password = "{Password}";

    var url = "https://api.business.kazanexpress.ru/api/oauth/token";
    var request = "grant_type=password&username=" + encodeURIComponent(login) + "&password=" + encodeURIComponent(password);

    debugger;
    var builder = MPS_CreateRestBuilder();
    builder.AddRequestHeader("Content-Type", "application/x-www-form-urlencoded");
    builder.AddRequestHeader("Authorization", "Basic a2F6YW5leHByZXNzOnNlY3JldEtleQ==");
    builder.withCredentials = true;
    builder.SendPost(url, request, MPS_GetTokenCallBack);
}


function MPS_GetTokenCallBack(responce) {
    MPS_PushLog("GetTokenCallBack");

    var dateTo = new Date();
    var dateFrom = dateTo.addDays(-80); //дней выгрузки

    dateTo = parseInt(dateTo.valueOf() / 1000);
    dateFrom = parseInt(dateFrom.valueOf() / 1000);

    var url = "https://api.business.kazanexpress.ru/api/seller/finance/orders?" +
        "size=1073741824" +
        "&page=0" +
        "&group=false" +
        "&dateFrom=" + dateFrom +
        "&dateTo=" + dateTo;

    var builder = MPS_CreateGetBuilder();
    builder.AddRequestHeader("Accept", "*/*");
    builder.AddRequestHeader("Access-Control-Request-Method", "GET");
    builder.AddRequestHeader("Access-Control-Request-Headers", "authorization");
    builder.AddRequestHeader("Sec-Fetch-Mode", "cors");
    builder.AddRequestHeader("Sec-Fetch-Site", "same-site");
    builder.AddRequestHeader("Sec-Fetch-Dest", "empty");
    builder.MethodName = "OPTIONS";

    builder.SendPost(url, MPS_CreateExportOptionCallback, { url: url, access_token: responce.access_token });
}

function MPS_CreateExportOptionCallback(responce, option) {
    MPS_PushLog("CreateExportOptionCallback");

    var builder = MPS_CreateGetBuilder();
    builder.AddRequestHeader("Accept", "application/json");
    builder.AddRequestHeader("Authorization", "Bearer " + option.access_token);
    builder.withCredentials = true;

    builder.SendPost(option.url, MPS_CreateExportGetCallback, option);
}

function MPS_CreateExportGetCallback(responce, option) {
    MPS_PushLog("CreateExportGetCallback");
    MPS_DownloadData(JSON.stringify(responce), "report.json", "application/octet-stream");
    console.log("StopAppScript");
}

MPS_Init();