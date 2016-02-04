var searchByClick = function () {
    
    var visibleSearch = $('#visibleSearch');

    if (visibleSearch.val() !== '') {//if someone entered manual search, clear it. 
        visibleSearch.val('');
        visibleSearch.keyup();
    }
    var currentSearch = $("input[name='searchText']");
    var currentSearchTerm = currentSearch.val();
    var newSearchTerm = this.value;
    if (currentSearchTerm) {
        var split = currentSearchTerm.split(' ');
        if (split.indexOf(newSearchTerm) == -1) {
            currentSearch.val(currentSearchTerm + ' ' + newSearchTerm);
        }
        else {
            var arrayLength = split.length;
            var searchTermAfterRemoval = '';
            for (var i = 0; i < arrayLength; i++) {
                if (split[i] !== newSearchTerm && split[i] !== ' ' && split[i]) {
                    searchTermAfterRemoval = searchTermAfterRemoval + split[i] + ' ';
                }
                currentSearch.val(searchTermAfterRemoval);

            }
        }
    }
    else {
        currentSearch.val(newSearchTerm);
    }

    currentSearch.keyup();
};

var main = function () {

    $('#loadTickets').click(function () {
        
        $(this).removeClass('btn-primary')
        $(this).addClass('btn-info');
        $(this).prop('value','Updating tickets...');
        $('*').fadeTo(2, 0.9)
        $('#spinner').show();
    });

    $('#submitComments').click(function () {

        $(this).removeClass('btn-primary')
        $(this).addClass('btn-info');
        $(this).prop('value', 'Submitting Comments...');
        $('*').fadeTo(2, 0.9)
        $('#spinner').show();
    });


    $('#submitComments').click(function () {

        $(this).removeClass('btn-primary')
        $(this).addClass('btn-info');
        $(this).prop('value', 'Submitting Comments...');
        $('*').fadeTo(2, 0.9)
        $('#spinner').show();
    });

    $('.select').click(searchByClick);

    
}

$(document).ready(main);
