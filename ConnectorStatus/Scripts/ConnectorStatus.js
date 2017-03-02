var filterClients = new Array();
var filterClasses = new Array();


function filterByArray(array) {
    if (array.length > 0) {
        $('#connectorStatusTable > tbody > tr').each(function () {
            var found = false;
            for (var i = 0; i < array.length; i++) {
                if ($(this).hasClass(array[i])) {
                    found = true;
                    break;
                }
            }
            if (!found) {
                $(this).hide();
            }

        });
    }
}

function filterByClass() {

    var visibleSearch = $('#visibleSearch');

    if (visibleSearch.val() !== '') {//if someone entered manual search, clear it. 
        visibleSearch.val('');
        visibleSearch.keyup();
    }

    $('#connectorStatusTable > tbody > tr').show();

    filterByArray(filterClasses);
    filterByArray(filterClients);

}

function searchByMultiFilter(searchString, enableSelect) {
    var scrumList = searchString;
    var selected = scrumList.split(' ');
    var length = selected.length;
    for (var i = 0; i < length; i++) {
        var tag = '#' + selected[i];
        if (enableSelect) {
            $(tag).removeClass('active');
            $(tag).click();
        } else {
            $(tag).addClass('active');
            $(tag).click();
        }
        
    }
}


var main = function () {


    $('#submitComments').click(function () {

        $(this).removeClass('btn-primary')
        $(this).addClass('btn-info');
        $(this).prop('value', 'Submitting Comments...');
        $('*').fadeTo(2, 0.9)
        $('#spinner').show();
    });


    $('.filter-button').click(function () {
        if (filterClients.indexOf(this.value) >= 0) {
            filterClients.splice(filterClients.indexOf(this.value), 1);
        } else {
            filterClients.push(this.value);
        }

        filterByClass();
    });

    $('.filter-button-status').click(function () {

        if (filterClasses.indexOf(this.value) >= 0) {
            filterClasses.splice(filterClasses.indexOf(this.value), 1);
        } else {
            filterClasses.push(this.value);
        }

        filterByClass();

    });

    $('.filter-button-group').click(function () {
        var clickedValue = this.value;
        var enableSelect = !$(this).hasClass('active');
        searchByMultiFilter(clickedValue, enableSelect);
    });

    

    
    
}

$(document).ready(main);
