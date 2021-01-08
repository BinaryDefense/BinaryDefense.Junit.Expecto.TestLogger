# ExpectoJunitTestLogger

---

## What is ExpectoJunitTestLogger?

BinaryDefense.Junit.Expecto.TestLogger is a logger for `dotnet test` that accepts expecto-formatted `TestResult`s and generates a gitlab-compatible junit xml report.

## Why use ExpectoJunitTestLogger?

This logger runs during the `dotnet test` step and is designed to specifically work with Expecto. It is also designed specifically to be combatible with Gitlab. Existing junit loggers or formatters, such as `Junit.Xml.TestLogger`, or Expecto's `--junit-summary` flag, either don't play well with Expecto or don't play well with Gitlab. 

In a more general sense, use this project if you want junit formatted reports in Gitlab and your test library outputs tests in a delimited format instead of the usual "class method" format.

---

<div class="row row-cols-1 row-cols-md-2">
  <div class="col mb-4">
    <div class="card h-100">
      <div class="card-body">
        <h5 class="card-title">Tutorials</h5>
        <p class="card-text">Takes you by the hand through a series of steps to create your first thing. </p>
      </div>
      <div class="card-footer text-right   border-top-0">
        <a href="{{siteBaseUrl}}/Tutorials/Getting_Started.html" class="btn btn-primary">Get started</a>
      </div>
    </div>
  </div>
  <div class="col mb-4">
    <div class="card h-100">
      <div class="card-body">
        <h5 class="card-title">How-To Guides</h5>
        <p class="card-text">Guides you through the steps involved in addressing key problems and use-cases. </p>
      </div>
      <div class="card-footer text-right   border-top-0">
        <a href="{{siteBaseUrl}}/How_Tos/Doing_A_Thing.html" class="btn btn-primary">Learn Usecases</a>
      </div>
    </div>
  </div>
  <div class="col mb-4 mb-md-0">
    <div class="card h-100">
      <div class="card-body">
        <h5 class="card-title">Explanations</h5>
        <p class="card-text">Discusses key topics and concepts at a fairly high level and provide useful background information and explanation..</p>
      </div>
      <div class="card-footer text-right   border-top-0">
        <a href="{{siteBaseUrl}}/Explanations/Background.html" class="btn btn-primary">Dive Deeper</a>
      </div>
    </div>
  </div>
  <div class="col">
    <div class="card h-100">
      <div class="card-body">
        <h5 class="card-title">Api Reference</h5>
        <p class="card-text">Contain technical reference for APIs.</p>
      </div>
      <div class="card-footer text-right   border-top-0">
        <a href="{{siteBaseUrl}}/Api_Reference/ExpectoJunitTestLogger/ExpectoJunitTestLogger.html" class="btn btn-primary">Read Api Docs</a>
      </div>
    </div>
  </div>
</div>
