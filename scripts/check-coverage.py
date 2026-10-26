#!/usr/bin/env python3
"""Fail when measured application coverage drops below the checked-in baseline."""
import json
import sys
from pathlib import Path
import xml.etree.ElementTree as ET


def main():
    baseline = json.loads((Path(__file__).parent / "coverage-baseline.json").read_text())
    report = ET.parse(sys.argv[1]).getroot()
    measured = {
        "line": 100 * float(report.attrib["line-rate"]),
        "branch": 100 * float(report.attrib["branch-rate"]),
    }
    failures = []
    for metric, value in measured.items():
        minimum = baseline[metric]
        print(f"{metric.capitalize()} coverage: {value:.2f}% (minimum {minimum:.2f}%, goal 95%+)")
        if value + 0.001 < minimum:
            failures.append(f"{metric} coverage {value:.2f}% is below {minimum:.2f}%")
    if failures:
        raise SystemExit("\n".join(failures))


if __name__ == "__main__":
    main()
