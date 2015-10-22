// This script handles showing and hiding the Create Books form
// and populating the autocomplete options for the user.
// Andrew Cooper, October 2015

var autocompleteData;
var autocompleteTerm;

// Disable submit with enter key, since most of the form is initially hidden
$("#submitBookForm").bind("keypress", function (e) {
    if (e.keyCode == 13) {
        $("#submitButton").attr('value');
        return false;
    }
});

// When the doc is ready (onload)
$(document).ready(function () {

    // Any problems, show the whole form. The errors variable is set by inline 
    // Razor markup, depending on the model state. If this fails, null ensures
    // that the page will load without showing a half-filled form with errors.
    if (errors) {
        $('.auto-hidden').show();
        $('.spinner').hide();
    }

    // Empty the text box, and setup the autocomplete handler
    $('#titleTextBox').value = "";
    $('#titleTextBox').autocomplete(
        {
            source: function (request, response) {
                $('.auto-hidden').hide();

                var titles = new Array();
                autocompleteTerm = request.term;
                // HTTP POST the Books Controller for the list of books
                $.post('/Books/GetAutocompleteList',
                        {
                            term: request.term
                        },
                function (data, status) {
                    autocompleteData = data;
                    for (var i = 0; i < data[0].length; i++) {
                        titles[i] = data[0][i];
                    }
                    titles[data[0].length] = "None of these...";
                    response(titles);
                });
                response(["..."]);
            },
            select: function (event, ui) {
                $('.auto-hidden').fadeIn();
                $('.spinner').hide();

                // If autocomplete worked out, let's use the details of that books
                if (ui.item.value == "None of these..." || ui.item.value == "...") {
                    ui.item.value = autocompleteTerm;
                    document.getElementById("authorTextBox").value = "";
                    document.getElementById("isbnTextBox").value = "";
                    document.getElementById("yearTextBox").value = "";
                    document.getElementById("editionTextBox").value = "";
                } else { // Otherwise empty the form and let the user fill it in manuallt
                    var title = ui.item.value;
                    var i = 0;
                    for (; autocompleteData[0][i] != title; i++) { }
                    document.getElementById("authorTextBox").value = autocompleteData[1][i];
                    document.getElementById("isbnTextBox").value = autocompleteData[2][i];
                    document.getElementById("yearTextBox").value = autocompleteData[3][i];
                    document.getElementById("editionTextBox").value = "1st";
                }
            }
        })
    
    // If the text box has been emptied by the user, fade the form away again for consistency.
    $('#titleTextBox').bind('input', function () {
        if ($('#titleTextBox').value == null) {
            $('.auto-hidden').fadeOut(200, function() {
                $('.spinner').show();
            })
        }
    });
});

