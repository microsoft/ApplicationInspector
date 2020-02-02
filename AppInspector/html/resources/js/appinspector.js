$(document).ready(() => {
    // Initialize Bootstrap
    $('[data-toggle="popover"]').popover();
    $('[data-toggle="tooltip"]').tooltip();


    /* Main Navigation Links: These are links that change the
     * visible section to something else, specified by the
     * `data-target` attribute of the link, which should usually
     * be the #div-identifier to be shown.
     */
    $('a.nav-button').on('click', (e) => {
        let current = $('body').data('visible_section') || '#page__report_overview';
        let target = $(e.target).data('target');
        if (!!target) {
            if (!!current) {
                $(current).addClass('d-none');
            }

            $(target).removeClass('d-none');
            $('body').data('visible_section', target);
        }
    });

    $('a.file_listing').on('click', (e) => {
        $('#editor-container').addClass('d-none');
        $('#file_listing_modal').modal();
    })

	/*
	 * When a user clicks on a file listing filename, load the data
	 * to show into the Ace editor in the popup dialog.
	 */
    $('#file_listing_modal').on('click', 'a.content-link', (e) => {
        const content = $(e.target).data('excerpt');
        const startLocationLine = $(e.target).data('startLocationLine');
        const editor = ace.edit("editor");

        editor.setOption('firstLineNumber', startLocationLine);
        editor.getSession().setValue(content);
        editor.resize();
        editor.scrollToLine(0);
        $('editor-container').removeClass('d-none');
    });

    const templateInsertion = new TemplateInsertion(data);
    templateInsertion.processSummaryPage();
    templateInsertion.processProfilePage();
});



class TemplateInsertion {
    constructor(data) {
        this.ap = data.AppProfile;
        this.mt = this.ap.MetaData;
        this.md = data.matchDetails;
    }

    processSummaryPage() {
        c3.generate({
            bindto: '#s_pi_analysis_chart',
            size: {
                width: 200
            },
            data: {
                columns: [
                    ['Analyzed', this.mt.filesAnalyzed],
                    ['Skipped', this.mt.filesSkipped],
                ],
                type: 'donut'
            },
            donut: {
                title: "Analyzed Files",
                label: {
                    format: function(value) { return value; }
                }
            }
        });

        c3.generate({
            bindto: '#s_pi_patterns_chart',
            size: {
                width: 200
            },
            data: {
                columns: [
                    ['Unique Matches', this.mt.uniqueMatchesCount],
                    ['Repeats', this.mt.totalMatchesCount - this.mt.uniqueMatchesCount],
                ],
                type: 'donut'
            },
            donut: {
                title: "Results",
                label: {
                    format: function(value) { return value; }
                }
            }
        });

      
        c3.generate({
            bindto: '#s_pi_languages_chart', 
            size: {
                width: 200
            },
            data: {
                columns: Object.entries(this.mt.languages),
                type: 'donut'
            },
            donut: {
                title: "Language Files",
                label: {
                    format: function(value) { return value; }
                }
            }
        });

        
        $('#s_pi_application_name').html(this.mt.applicationName);
        $('#s_pi_version').html(this.mt.sourceVersion);
        $('#s_pi_description').html(this.mt.description || 'No description available.');
        $('#s_pi_source_path').html(this.ap.sourcePath);
        $('#s_pi_author').html(this.mt.authors || 'No author found.');
        $('#s_pi_date_scanned').html(this.ap.dateScanned);
       
    }

    combineConfidence(a, b) {
        if (a && !b) return a;
        if (b && !a) return b;
        if (!a && !b) return 'Low';

        const _a = a.toLowerCase();
        const _b = b.toLowerCase();
        const map = { 'low': 1, 'medium': 2, 'high': 4 };
        if (map[_a] > map[_b]) { return a; }
        return b;
    }

    show_file_listing(e) {
        let $_tr = e.target.nodeName == 'TR' ? $(e.target) : $(e.target).closest('tr');
        let ruleid = $_tr.find('a').data('ruleid');
        let $this = e.data.obj;

        $('#file_listing_modal ul').empty();
        $('editor-container').addClass('d-none');

        const removePrefix = (fn) => {
            if (!fn.startsWith($this.ap.sourcePath)) {
                return fn;
            }
            return fn.slice($this.ap.sourcePath.length);
        }

        for (let match of $this.md) {
            let excerpt = atob(match.excerpt || '') || match.sample;
            if (match.ruleId === ruleid || match.ruleName === ruleid) {
                let $li = $('<li>');
                let $a = $('<a>');
                $a.addClass('content-link')
                    .attr('href', '#')
                    .data('excerpt', excerpt)
                    .data('startLocationLine', match.startLocationLine - 3)
                    .text(removePrefix(match.fileName));
                $li.append($a);
                $('#file_listing_modal ul').append($li);

                $('#match-line-number').text('Line number: ' + match.startLocationLine.toString());
            }
        }
        $('#file_listing_modal').on('shown.bs.modal', function (e) {
            $('a.content-link').first().trigger('click');
        });

       
        $('#file_listing_modal').modal();

        
    }

    /*
     * Builds the Profile page, including event handlers.
     */
    processProfilePage() {

        // Process each icon with the 'feature_icon' class.
        $('a.feature_icon').each((idx, elt) => {
            const targetRegexValue = $(elt).data('target');
            if (!targetRegexValue) {
                $(elt).addClass('disabled');        // Disable icon if no target exists
                return;
            }

            let foundTag = false;
            const targetRegex = new RegExp(targetRegexValue, 'i');

            // We have a target, treated as a regular expression.
            // We're goint to go through all results (this.md) and if it contains
            // a tag that matches what we're looking for, we'll keep that icon visible.
            search_loop:
            for (let match of this.md) {
                for (let tag of match.tags) {
                    if (targetRegex.exec(tag)) {
                        foundTag = true;        // We have at least one match for this icon
                        break search_loop;
                    }
                }
            }
            if (!foundTag) {
                $(elt).addClass('disabled');
            }
        });

       
        // Process each icon with the 'feature_icon' class.
        $('a.confidence_image').each((idx, elt) => {
            const confidenceValue = $(elt).data('target');

            var src;
            if (confidenceValue == "High")
                src = "html/conf-high.png";
            else if (confidenceValue == "Medium")
                src = "html/conf-medium.png";
            else
                src = "html/conf-low.png";

            $(elt).src = src;
        })


        // Event handler for visible icons
        $('a.feature_icon').on('click', (e) => {
            const targetRegexValue = $(e.target).closest('a').data('target');
            if (!targetRegexValue) {
                console.log('Error: No target regular expression. This probably indicates a bug.');
                return;
            }
            const targetRegex = new RegExp(targetRegexValue, 'i');
            let identifiedRules = {};
            for (let match of this.md) {
                for (let tag of match.tags) {
                    if (targetRegex.exec(tag)) {
                        identifiedRules[match.ruleName] = this.combineConfidence(identifiedRules[match.ruleName], match.confidence);
                        break;  // Only break out of inner loop, we only need one match per tag set
                    }
                }
            }

            const $tbody = $('#features table tbody');
            $tbody.empty();
            
            // Now we iterate through all of the rules that relate to this icon
            for (let [rule, confidence] of Object.entries(identifiedRules))
            {
                // I'm sorry, this is pretty ugly. If you'd like to clean this up, I'd be
                // happy to take a pull request.

                let $tr = $('<tr>');
                $tr.on('click', 'td', { 'obj': this }, this.show_file_listing);
                let $td0 = $('<td>');
                let $td0a = $('<a>');
                $td0a.attr('href', '#');
                $td0a.data('ruleid', rule);    // BUG: This should be the rule id, not the name
                $td0a.text(rule);
                //$td0a.on('click', {'obj': this }, this.show_file_listing);
                $td0.append($td0a);

                //let $td1 = $('<td>');
                //let $td1d = $('<div>');
                //$td1d.addClass('progress');
                //$td1d.css('width', '120px');
                //let $td1dp = $('<div>');
                //$td1dp.addClass('progress-bar')
                //    .addClass('bg-info')
                //    .attr('role', 'progressbar')
                //    .css('width', '100%')
                //    .attr('aria-valuenow', 100)
                //    .attr('aria-valuemin', 0)
                //    .attr('aria-valuemax', 100)
                //    .text('High');
                //$td1d.append($td1dp);
                //$td1.append($td1d);
                $tr.append($td0)
                //$tr.append($td1);
                $tbody.append($tr);
            }
        });
    }
}


function SortbyConfidence() {
    $('#divconf').attr('style', 'display:normal;border:none');
    $('#divsev').attr('style', 'display:none');
    $('#divtag').attr('style', 'display:none');
}

function SortbySeverity() {
    $('#divsev').attr('style', 'display:normal;border:none');
    $('#divconf').attr('style', 'display:none');
    $('#divtag').attr('style', 'display:none');
}

function SortbyTags() {
    $('#divtag').attr('style', 'display:normal;border:none');
    $('#divsev').attr('style', 'display:none');
    $('#divconf').attr('style', 'display:none');
}