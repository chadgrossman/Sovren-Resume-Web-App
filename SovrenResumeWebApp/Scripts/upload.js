var ResumeObject = {
    "RecruiterName": '',
    "Resumes": []
};
var ParsedResumes = {
    "Resumes": {
        "JobOrder": "",
        "ReturnObjects": []
    }
};
var ErrorResumes;
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
        };
        if ($.inArray(reader.file, fileNames) === -1 && itemsTotal < 25) {
            fileNames.push(reader.file);
            itemsTotal++;
            reader.readAsArrayBuffer(files[i]);
        }
    }

    displayFileNames();
    document.getElementById("filePicker").value = "";
    $(".file-upload .file-text").text(itemsTotal + " file(s) selected");
};

if (window.File && window.FileReader && window.FileList && window.Blob) {
    var filePicker = document.getElementById("filePicker");
    if (filePicker) {
        filePicker.addEventListener("change", handleFileSelect, false);
    }
} else {
    alert("The File APIs are not fully supported in this browser.");
}

$(".reset").click(function () {
    clearResumes();
});

$(".resume-submit").click(function () {
    if (ResumeObject.Resumes.length > 0) {
        loading();
        sovrenResumeApi(ResumeObject);
    }
});

$(".save").click(function () {
    if (checkInputFields()) {
        loading();
        formatUpsert();
        recordCreateAndUpdate();
    }
});

/* Page Methods */

function displayFileNames() {
    var allFiles = '';
    for (i = 0; i < fileNames.length; i++) {
        var fileFormat = '<div class="file-name-box"><p>' + fileNames[i] + '</p></div>';
        allFiles += fileFormat;
    }
    $("#file-names").html(allFiles);
}

function displayResults() {
    columns = "";
    $.each(ParsedResumes.Resumes.ReturnObjects, function (i, val) {
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
            fileName +
            '" disabled>' +
            '    </div>' +
            '    <div class="col-md-2 col-sm-6">' +
            '        <input type="text" class="form-control first-name-input" value="' +
            toTitleCase(firstName) +
            '">' +
            '    </div>' +
            '    <div class="col-md-2 col-sm-6">' +
            '        <input type="text" class="form-control last-name-input" value="' +
            toTitleCase(lastName) +
            '">' +
            '    </div>' +
            '    <div class="col-md-4 col-sm-6">' +
            '        <input type="email" class="form-control email-input" value="' +
            email.toLowerCase() +
            '">' +
            '    </div>' +
            '    <div class="col-md-2 col-sm-6">' +
            '        <input type="tel" class="form-control phone-input bfh-phone" data-country="US" value="' +
            phone.replace(/\D/g, '') +
            '">' +
            '    </div>' +
            '</div>';
    });

    $.each(ErrorResumes, function (i, val) {
        columns +=
            '<div class="row bottom-buffer errors-body">' +
            '    <div class="col-md-4 col-sm-6">' +
            '        <input type="text" class="form-control error-msg" value="' +
            ResumeObject.Resumes[val.ResumeIndex].FileName +
            '" disabled>' +
            '    </div>' +
            '    <div class="col-md-8 col-sm-6">' +
            '        <input type="text" class="form-control error-msg" value="' +
            val.ErrorMessage +
            '" disabled>' +
            '    </div>' +
            '</div>';
    });
    $(".header").after(columns);
    $(".phone-input").inputmask({ "mask": "(999) 999-9999" });
}

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
}

function formatUpsert() {
    var formResults = $(".results-body");
    for (i = 0; i < formResults.length; i++) {
        var resume = ParsedResumes.Resumes.ReturnObjects[i];
        resume.FirstName = $(formResults[i]).find(".first-name-input").val();
        resume.LastName = $(formResults[i]).find(".last-name-input").val();
        resume.EmailAddress = $(formResults[i]).find(".email-input").val();
        resume.PhoneNumber = $(formResults[i]).find(".phone-input").val();
        
        ParsedResumes.Resumes.ReturnObjects[i] = resume;
    }
    ParsedResumes.Resumes.JobOrder = $("#job-order").val().replace(/\D/g, '');
}

function loading() {
    $(".loading").toggleClass('hidden');
    $(".spinner").toggleClass('hidden');
}

function transition() {
    $("#results").toggle();
    $("#data-entry").toggle();
}

function clearResumes() {
    $(".results-body").remove();
    ResumeObject = {
        RecruiterName: "{!$User.FirstName} {!$User.LastName}",
        Resumes: []
    };
    fileNames = [];
    fileHolder = null;
    itemsProcessed = 0;
    itemsTotal = 0;
    $(".file-upload .file-text").text('Drag files here or click to upload.');
    $("#file-names").html('');
}

function sortResults(prop, asc) {
    ParsedResumes.Resumes.ReturnObjects = ParsedResumes.Resumes.ReturnObjects.sort(function (a, b) {
        if (asc) {
            return a[prop] > b[prop] ? 1 : a[prop] < b[prop] ? -1 : 0;
        } else {
            return b[prop] > a[prop] ? 1 : b[prop] < a[prop] ? -1 : 0;
        }
    });
}

function toTitleCase(str) {
    return str.replace(/\b\w+/g, function (txt) { return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase(); });
}

/* AJAX calls to controller */
var sovrenResumeApi = function (json) {
    $.ajax({
        url: '/home/sovrenresumeapi',
        type: 'POST',
        data: { resumeRequest: json },
        success: function (data) {
            ParsedResumes.Resumes.ReturnObjects = JSON.parse(data).ReturnObjects;
            ErrorResumes = JSON.parse(data).Errors;
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

var recordCreateAndUpdate = function () {
    $.ajax({
        url: '/home/salesforceupsert',
        type: 'POST',
        data: {
            "resumes": ParsedResumes.Resumes
        },
        success: function (data) {
            clearResumes();
            transition();
            loading();
        },
        error: function (error) {
            if (error.statusCode === 401 || error.responseText.includes('expired access/refresh token')) {
                location.reload(true);
            }
            console.log(error);
            $("#input-errors").html("An error occurred while saving the resumes.");
            loading();
        }
    });
};