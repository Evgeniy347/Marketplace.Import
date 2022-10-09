
function MPS_Init() {
    console.log("MPS_Init")

    if (!window.MPS_Context)
        return;

    if (location.href.startsWith("https://seller.wildberries.ru/login")) {
        //Ничего не делаем, если попали сюда то ждем аутентификацию
        MPS_PushLog("StartAuth");
        console.log("EnableBrowser");
    }
    else if (location.href.startsWith("https://seller.wildberries.ru/analytics")) {
        if (!window.document.body.StartAuthorization) {
            window.document.body.StartAuthorization = true;
            console.log("DisableBrowser");

            setTimeout(function () { MPS_CreateExport(); }, 1000);
        }
    }
    else {
        //если ничего из вышеперечисленного то вызывает повтроно, возможно будет редирект
        setTimeout(MPS_Init, 100);
    }
}

function MPS_CreateExport() {
    var supplierid = MPS_GetParams("SupplierID"); 

    if (supplierid) {
        MPS_SetCookie("x-supplier-id", supplierid);
        MPS_SetCookie("x-supplier-id-external", supplierid);
    }

    setTimeout(function () { MPS_CreateConsolidatedExport(); }, 1000);
    setTimeout(function () { MPS_CreateWeeklydynamicsExport(); }, 1500);
}

function MPS_CreateConsolidatedExport() {
    MPS_PushLog("MPS_CreateConsolidatedExport");

    var url = "https://seller.wildberries.ru/ns/consolidated/analytics-back/api/v1/consolidated-table-excel?isCommission=2";

    var params = {
        "filters": ["ordered", "paymentSalesRub", "paymentSalesPiece", "marginRate", "numberOrders",
            "goodsOrdered", "receiptsRub", "receiptsPiece", "turnover", "buybackPercentage",
            "logisticsCost", "storageCost", "surcharges", "salesRewards", "totalToTransfer"]
    }

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateConsolidatedCallback);
}

function MPS_CreateConsolidatedCallback(responce) {
    MPS_PushLog("MPS_CreateConsolidatedCallback");
    console.log(responce);

    MPS_DownloadBase64Data(responce.data.file, "Consolidated.XLSX", "application/octet-stream");
    window.MPS_Context.ConsolidatedDownload = true;
    MPS_CheckStopScript();
}

//****//

function MPS_CreateWeeklydynamicsExport() {
    MPS_PushLog("MPS_CreateWeeklydynamicsExport");

    var url = "https://seller.wildberries.ru/ns/weeklydynamics/analytics-back/api/v1/weekly-report-table-excel";

    var dateTo = new Date();
    var dateFrom = dateTo.addDays(-40); // 40 дней от текущей даты

    var params = {
        "brandID": 0, "contractID": -100, "officeID": -100,
        "dateFrom": dateFrom.toStringMPS("dd.MM.yy"),
        "dateTo": dateTo.toStringMPS("dd.MM.yy")
    }

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateWeeklydynamicsCallback);
}

function MPS_CreateWeeklydynamicsCallback(responce) {
    MPS_PushLog("MPS_CreateWeeklydynamicsCallback");
    console.log(responce);

    MPS_DownloadBase64Data(responce.data.excelReportWeeklyTable, "Weeklydynamics.XLSX", "application/octet-stream");
    window.MPS_Context.WeeklydynamicsDownload = true;
    MPS_CheckStopScript();
}

function MPS_CheckStopScript() {
    if (window.MPS_Context.WeeklydynamicsDownload && window.MPS_Context.ConsolidatedDownload)
        console.log("StopAppScript");
}

MPS_Init();