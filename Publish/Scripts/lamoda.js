
function MPS_Init() {

    if (!window.MPS_Context)
        return;

    setTimeout(function () {
        if (!window.document.body.StartAuthorization) {
            window.document.body.StartAuthorization = true;
            MPS_PushLog("StartAuthorizationLamoda");
            MPS_GetToken();
        }
    }, 1000);
}

function MPS_GetToken() {

    var login = "{Login}";
    var password = "{Password}";

    var url = "https://partner.lamoda.ru/api/token";
    var request = {
        "grant_type": "password", "username": login, "password": password
    }

    var builder = MPS_CreateRestBuilder();
    builder.SendPost(url, request, MPS_GetTokenCallBack);
}


function MPS_GetTokenCallBack(responce) {
    MPS_PushLog("GetTokenCallBack");

    var url = "https://partner.lamoda.ru/api/v1/exports";
    var endDate = new Date();
    var startDate = endDate.addDays(-1);
    endDate = startDate;

    var filterDate = MPS_DateLamodaFormat(startDate) + "%2C" + MPS_DateLamodaFormat(endDate, true);
    var request = {
        "filter": "createdAt%26gt%3B%3D%26lt%3B" + filterDate,
        "type": "orders_and_items",
        "format": "csv",
        "sort": "-createdAt",
        "description": null
    };

    var builder = MPS_CreateRestBuilder();
    builder.SuccessStatus.push(202);

    builder.AddRequestHeader("Authorization", "Bearer " + responce.access_token);
    builder.SendPost(url, request, MPS_CreateExportCallback, responce.access_token);
}

function MPS_DateLamodaFormat(d, end) {
    //2022 09 01 (000000 or 235959)
    var datestring = d.toStringMPS("yyyyMMdd");
    datestring += end ? "235959" : "000000";
    return datestring;
}

function MPS_CreateExportCallback(responce, access_token) {
    MPS_PushLog("CreateExportCallback");

    var url = "https://partner.lamoda.ru/api/v1/exports/" + responce.id + "/download";

    var params = {
        access_token: access_token,
        FileURL: url,
    };
    MPS_DownloadExport(params);
}

function MPS_DownloadExport(params) {
    MPS_PushLog("DownloadExport");

    var builder = MPS_CreateGetBuilder();

    builder.ResponseType = "arraybuffer";
    builder.AddRequestHeader("Authorization", "Bearer " + params.access_token);
    builder.SuccessStatus.push(404);
    builder.SendPost(params.FileURL, MPS_DownloadExportCallback, params);
}

function MPS_DownloadExportCallback(responce, params) {
    MPS_PushLog("DownloadExportCallback");

    if (!responce || responce.byteLength == 0) {
        setTimeout(MPS_DownloadExport, 1000, params);
        return;
    }

    MPS_DownloadData(responce, "report.csv", "application/octet-stream");
    console.log("StopAppScript");
} 

MPS_Init();