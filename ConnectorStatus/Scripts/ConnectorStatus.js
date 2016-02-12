function searchByMultiFilter(searchString){
    var scrumList = searchString;
    var selected = scrumList.split(' ');
    var length = selected.length;
    for (var i = 0; i < length; i++) {
        var tag = '#' + selected[i];
        $(tag).click();
    }
}

var searchByClick = function () {
    
    var visibleSearch = $('#visibleSearch');

    if (visibleSearch.val() !== '') {//if someone entered manual search, clear it. 
        visibleSearch.val('');
        visibleSearch.keyup();
    }

    var currentSearch = $("input[name='searchText']");
    var currentSearchTerm = currentSearch.val();
    var clicked = this.value;
    var newSearchTerm = '';
    var removing = (currentSearchTerm && currentSearchTerm.indexOf(clicked) >= 0);
    if (!removing){
        newSearchTerm += clicked + ' ';
    }
    
    
    $('.select.active').each(function () {
        newSearchTerm += this.value + ' ';
    });

    if (removing) {
        newSearchTerm = newSearchTerm.replace(clicked + ' ', '').replace(' ' + clicked, '');
    }

    currentSearch.val(newSearchTerm);
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

    $('.selectGroup').click(function(){
        var clickedValue = this.value;
        searchByMultiFilter(clickedValue);
    });

    
}

$(document).ready(main);
