// This script uses jQuery UI to offer search suggestions
// to a user based on the titles currently available in the
// database.
// Andrew Cooper - October 2015

// Autocomplete available book titles
$(document).ready(function () {
    $('#indexSearchControl').autocomplete(
        {
            source: function (request, response) {
                var titles = searchExamples.filter(function (value, index, ar) {
                    if (value.toLowerCase().search(request.term.toLowerCase()) == -1) {
                        return false;
                    }
                    return true;
                });
                if (titles.length > 5) {
                    titles = titles.slice(0, 5);
                }
                response(titles);
            }
        });
});