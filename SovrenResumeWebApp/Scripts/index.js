var ResumeObject = {
    "RecruiterName": userId,
    "Resumes": []
};
var ParsedResumes = {
    "resumes": {
        "JobOrder": "",
        "ReturnObjects": []
    }
};
var fileNames = [];
var itemsProcessed = 0;
var itemsTotal = 0;

/* Page Behaivor */

var handleFileSelect = function (evt) {
    var files = evt.target.files;

    for (i = 0; i < files.length; i++) {
        var reader = new FileReader();
        reader.file = files[i].name;
        reader.onload = function (event) {
            var base64Text = Base64.encodeArray(event.target.result);
            ResumeObject.Resumes.push({
                EmailAddress: "",
                SalesforceId: "",
                DocumentAsBase64String: base64Text,
                FileName: event.target.file
            });
            // fileNames.push(event.target.file);

            itemsProcessed++;
            if (itemsProcessed === itemsTotal) {

            }
        };
        if ($.inArray(reader.file, fileNames) === -1 && itemsTotal < 25) {
            fileNames.push(reader.file);
            itemsTotal++;
            reader.readAsArrayBuffer(files[i]);
        }
    }

    $(".file-upload p").text(itemsTotal + " file(s) selected");
};

if (window.File && window.FileReader && window.FileList && window.Blob) {
    var filePicker = document.getElementById("filePicker");
    if (filePicker) {
        filePicker.addEventListener("change", handleFileSelect, false);
    }
} else {
    alert("The File APIs are not fully supported in this browser.");
}

$(".resume-submit").click(function () {
    if (ResumeObject.Resumes.length > 0) {
        loading();
        initialPostRequest(ResumeObject);
    }
});

$(".save").click(function () {
    if (checkInputFields()) {
        loading();
        formatUpsert();
        sovrenLookup();
    }
});

/* Page Methods */

function displayResults() {
    columns = "";
    $.each(ParsedResumes.resumes.ReturnObjects, function (i, val) {
        var fileName = val.FileName !== null ? val.FileName : '';
        var firstName = val.FirstName !== null ? val.FirstName : '';
        var lastName = val.LastName !== null ? val.LastName : '';
        var email = val.EmailAddresses && val.EmailAddresses.length ? val.EmailAddresses[0] : '';
        val.EmailAddress = email;
        delete val.EmailAddresses;
        var phone = val.PhoneNumbers && val.PhoneNumbers.length ? val.PhoneNumbers[0] : '';
        val.PhoneNumber = phone;
        delete val.PhoneNumbers;
        columns +=
            '<div class="row bottom-buffer results-body">' +
            '    <div class="col-md-2 col-sm-12">' +
            '        <input type="text" class="form-control file-name-output" value="' +
            // fileNames[i] +
            fileName +
            '" disabled>' +
            '    </div>' +
            '    <div class="col-md-2 col-sm-6">' +
            '        <input type="text" class="form-control first-name-input" value="' +
            firstName +
            '">' +
            '    </div>' +
            '    <div class="col-md-2 col-sm-6">' +
            '        <input type="text" class="form-control last-name-input" value="' +
            lastName +
            '">' +
            '    </div>' +
            '    <div class="col-md-4 col-sm-6">' +
            '        <input type="email" class="form-control email-input" value="' +
            email +
            '">' +
            '    </div>' +
            '    <div class="col-md-2 col-sm-6">' +
            '        <input type="tel" class="form-control phone-input bfh-phone" data-country="US" value="' +
            phone.replace(/\D/g, '') +
            '">' +
            '    </div>' +
            '</div>';
    });
    $.each(results.Errors, function (i, val) {
        columns +=
            '<div class="row bottom-buffer errors-body">' +
            '    <div class="col-md-4 col-sm-6">' +
            '        <input type="text" class="form-control error-msg" value="' +
            ResumeObject.Resumes[val.ResumeIndex].FileName +
            '">' +
            "    </div>" +
            '    <div class="col-md-8 col-sm-6">' +
            '        <input type="text" class="form-control error-msg" value="' +
            val.ErrorMessage +
            '">' +
            "    </div>" +
            "</div>";
    });
    $(".header").after(columns);
    $(".phone-input").inputmask({ "mask": "(999) 999-9999" });
};

function checkInputFields() {
    var proceed = true;
    var inputFields = $("input[class*='input']");
    for (i = 0; i < inputFields.length; i++) {
        var input = inputFields[i];
        if (input.value === "") {
            proceed = false;
            $(input).css('border', '1px solid red');
            $("#input-errors").html('Please fill out all of the highlighted fields');
        }
    }
    return proceed;
};

function formatUpsert() {
    var formResults = $(".results-body");
    for (i = 0; i < formResults.length; i++) {
        var resume = ParsedResumes.resumes.ReturnObjects[i];
        resume.FirstName = $(formResults[i]).find(".first-name-input").val();
        resume.LastName = $(formResults[i]).find(".last-name-input").val();
        resume.EmailAddress = $(formResults[i]).find(".email-input").val();
        resume.PhoneNumber = $(formResults[i]).find(".phone-input").val();
        
        ParsedResumes.resumes.ReturnObjects[i] = resume;
    };
}

function loading() {
    $(".loading").removeClass('hidden');
    $(".spinner").removeClass('hidden');
};

function transition() {
    $("#results").toggle();
    $("#data-entry").toggle();
};

function clearResumes() {
    $(".results-body").remove();
    ResumeObject = {
        RecruiterName: "{!$User.FirstName} {!$User.LastName}",
        Resumes: []
    };
    $(".file-upload p").text('Drag your files here or click in this area.');
    var fileNames = [];
    var itemsProcessed = 0;
    var itemsTotal = 0;
};

function sortResults(prop, asc) {
    ParsedResumes.resumes.ReturnObjects = ParsedResumes.resumes.ReturnObjects.sort(function (a, b) {
        if (asc) {
            return (a[prop] > b[prop]) ? 1 : ((a[prop] < b[prop]) ? -1 : 0);
        } else {
            return (b[prop] > a[prop]) ? 1 : ((b[prop] < a[prop]) ? -1 : 0);
        }
    });
}

/* AJAX calls to controller */
var initialPostRequest = function (json) {
    $.ajax({
        url: '/home/sovrenresumeapi',
        type: 'POST',
        data: { resumeRequest: JSON.stringify(json) },
        success: function (data) {
            ParsedResumes.resumes.ReturnObjects = JSON.parse(data).ReturnObjects;
            sortResults('FileName', true);
            displayResults();
            loading();
            transition();
        },
        error: function (error) {
            console.log(error);
            $("#parse-errors").html('An error occurred while parsing the resumes.');
            loading();
        }
    });
};

var updatePostRequest = function (json) {
    $.ajax({
        url: '/home/sovrenresumeupdateapi',
        type: 'POST',
        data: { resumeUpdate: json },
        success: function (data) {
            clearResumes();
            transition();
            loading();
        },
        error: function (error) {
            console.log(error);
            $("#input-errors").html('An error occurred sending updates.');
            loading();
        }
    });
};

var sovrenLookup = function () {
    var job = $("#job-order").val();
    if (job !== "") {
        $.ajax({
            url: '/home/salesforcelookup',
            type: 'POST',
            data: {
                instanceUrl: instance,
                token: accessToken,
                jobOrder: job.replace(/\D/g, '')
            },
            success: function (data) {
                ParsedResumes.resumes.JobOrder = data.replace(/['"]/g, '');
                sovrenUpsert();
            },
            error: function (error) {
                $(".job-order").css('border', '1px solid red');
                $("#input-errors").html('Job Order not found. Please check the Job Order and try again.');
                loading();
            }
        });
    } else {
        console.log('No Job Order');
        sovrenUpsert();
    }
};

var sovrenUpsert = function () {
    var jsonInfo = JSON.stringify(ParsedResumes);
    $.ajax({
        url: '/home/salesforceupsert',
        type: 'POST',
        data: {
            instanceUrl: instance,
            token: accessToken,
            parsedResumes: jsonInfo
        },
        success: function (data) {
            updatePostRequest(data);
        },
        error: function (error) {
            console.log(error);
            loading();
        }
    });
};