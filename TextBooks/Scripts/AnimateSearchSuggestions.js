// This script animates the search suggestions on the home page
// Additionally, it requests a list of book titles currently available
// from the database to use as the animated search suggestions.
// Andrew Cooper - October 2015

// Default search examples
searchExamples = [
    "A Brief History of the Universe",
    "A Brief History of Time",
    "Stephen Hawking",
    "Learning Python",
    "Calculus 101",
    "Mark Lutz and David Ascher",
    "Economics for the New Student",
    "All About Water",
    "Networks and Their Uses",
    "Mysteries of Human Anatomy",
    "Calculus - Early Transcendentals",
    "Anton Bivens Davis",
    "Essential Business Process Modelling",
    "Publication Manual of the American Psychological Association"
];

// Update search examples to match actual available books
$.post('/Books/ListAllBookTitles',
    {
        quantity: 0
    },
    function (data, status) {
        searchExamples = data;
    }
);

// Generate a random number within the range of the examples array
var randomNumber = function () {
    return Math.floor(Math.random() * searchExamples.length);
}

// Select a new example title to display
var newSelection = function () {
    return "eg, " + searchExamples[randomNumber()];
}

// Start with an example title
document.getElementById("indexSearchControl").placeholder = newSelection();

// Variables for animation 
currentSuggestion = newSelection();
index = 0;

// Animate "typing" the title one character at a time
addLetter = function () {
    index++;
    if (index < 35) {
        currentSubstring = currentSuggestion.substr(0, index);
    } else {
        currentSubstring = currentSuggestion.substr(index - 34, index);
    }
    document.getElementById("indexSearchControl").placeholder = currentSubstring;

    if (index >= currentSuggestion.length) {
        currentSuggestion = newSelection();
        index = 0;
        window.setTimeout(addLetter, 7000);
    } else {
        if (currentSubstring.substring(index - 1, index) == " ") {
            window.setTimeout(addLetter, 150);
        } else {
            window.setTimeout(addLetter, 70);
        }
    }
}

// Start the animation
window.setTimeout(addLetter, 2000);