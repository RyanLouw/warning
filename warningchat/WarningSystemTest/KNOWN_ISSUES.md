# Defects exposed by the tests

Application behaviour is intentionally left unchanged. These are expected rules,
not changes to make the suite green. The two previously requested skipped email
cases remain skipped; newly failing regression tests remain enabled.

## Confirmed by unit execution

| Test | Expected | Current result |
|---|---|---|
| `TaskListTests.Hidden_issue_is_still_visible_to_another_team_lead_in_the_hierarchy` | HideFromTeamLead hides the issue from its creator, while another team lead with reporting access can still see it. | Filtering removes it from every User-role team lead. |
| `TaskListTests.Chairperson_cannot_see_a_hearing_issue_type` | Chairperson cannot see an issue whose type is Hearing. | The filter checks the status text Hearing, so a Hearing type with status Validated remains visible. |
| `EmailTests.Information_emails_use_packaged_templates_and_encode_notes(request: true)` | A request for more information renders the existing template and sends a captured test email. | MoreInformationRequired.html is not embedded in the core assembly. |
| `EmailTests.Information_emails_use_packaged_templates_and_encode_notes(request: false)` | Added information renders its existing template and sends a captured test email. | MoreInformationAdded.html is not embedded in the core assembly. |

The core also does not embed IssueUpdated.html. The two team-lead decision-email
unit cases for it are the cases previously skipped at user request.

## Browser/database cases needing local execution

These are checks suggested by source inspection, not verified UI failures here:

- Legal should see Validate for an Issued issue, but the view tests Completed.
- Record Decision should disappear after the decision is recorded. Its current
  pattern expression may keep it visible for Pending/Issued issues.
- Validated issues must reject team-lead writes, including direct submissions.
  The evidence-save path currently checks existence without a read-only check.
- A creator must not open an issue hidden from them using a direct wizard or
  team-lead URL, even if it is absent from the home task list.
- Roles forbidden to create issues/discussions should not see those creation
  controls on home. They currently appear without a role condition.
- The report should offer the Issued status filter.
- Saving a selected local calendar day should preserve that day in South Africa.
- Every selected category should be persisted when a draft is created; the
  category-link additions currently have no save before the transaction commit.

The browser/SQL suite also covers ordinary successful flows, cancellations,
invalid input, permissions, reload persistence, default category questions and
SQL comparisons. Run it through Aspire on the working local/build-server setup
before deciding which additional application fixes are required.
