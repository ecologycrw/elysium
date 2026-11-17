namespace Elysium.Checks.Tests;

public sealed class DocsCheckTests
{
    private const string Plan = "# Plano\n";

    [Fact]
    public void Valid_repository_has_no_findings()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("README.md", "See [status](docs/STATUS.md#status) and [the docs folder](docs/).\n");

        Assert.Empty(repo.Docs());
    }

    [Fact]
    public void Broken_link_is_reported()
    {
        using var repo = TempRepository.WithValidDocs().Write("README.md", "See [missing](docs/MISSING.md).\n");

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("broken link 'docs/MISSING.md'", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_anchor_is_reported_and_accented_anchor_is_found()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("docs/CONVENTIONS.md", "# Convenções\n\n## Regra de três\n")
            .Write("README.md", "[ok](docs/CONVENTIONS.md#regra-de-três) [bad](docs/CONVENTIONS.md#regra-de-dois)\n");

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("#regra-de-dois", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Link_outside_the_repository_is_reported()
    {
        using var repo = TempRepository.WithValidDocs().Write("README.md", "[out](../outside.md)\n");

        Assert.Contains(repo.Docs(), finding => finding.Message.Contains("outside the repository", StringComparison.Ordinal));
    }

    [Fact]
    public void Spec_with_invalid_status_is_reported()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("specs/9999-teste/spec.md", Spec("pronta", "- CA-01: algo"))
            .Write("specs/9999-teste/plan.md", Plan);

        Assert.Contains(repo.Docs(), finding => finding.Message.Contains("'status' must be one of", StringComparison.Ordinal));
    }

    [Fact]
    public void Spec_folder_with_bad_name_is_reported()
    {
        using var repo = TempRepository.WithValidDocs().Write("specs/Fatia_Zero/spec.md", Spec("rascunho", string.Empty));

        Assert.Contains(repo.Docs(), finding => finding.Message.Contains("NNNN-name-in-kebab-case", StringComparison.Ordinal));
    }

    [Fact]
    public void Concluded_spec_without_report_is_reported()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("specs/9999-teste/spec.md", Spec("concluida", "- CA-01: algo"))
            .Write("specs/9999-teste/plan.md", Plan)
            .Write("specs/9999-teste/tasks.md", "# Tarefas\n");

        Assert.Contains(repo.Docs(), finding => finding.Message.Contains("requires report.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Approved_spec_without_criteria_is_reported()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("specs/9999-teste/spec.md", Spec("aprovada", "Sem critérios."))
            .Write("specs/9999-teste/plan.md", Plan);

        Assert.Contains(repo.Docs(), finding => finding.Message.Contains("at least one acceptance criterion", StringComparison.Ordinal));
    }

    [Fact]
    public void Duplicate_criterion_is_reported()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("specs/9999-teste/spec.md", Spec("aprovada", "- CA-01: um\n- CA-01: outro"))
            .Write("specs/9999-teste/plan.md", Plan);

        Assert.Contains(repo.Docs(), finding => finding.Message.Contains("CA-01 is defined more than once", StringComparison.Ordinal));
    }

    [Fact]
    public void Criterion_without_proof_is_reported()
    {
        using var repo = ConcludedSpec("| CA-01 | teste | ok |");

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("CA-01 has no proof", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Criterion_proved_by_a_test_trait_passes()
    {
        using var repo = ConcludedSpec("| CA-01 | teste automático | ok |")
            .Write("tests/Fake.Tests/MoveTests.cs", """
                public class MoveTests
                {
                    [Fact]
                    [Trait("CA", "9999-CA-01")]
                    public void Works() { }
                }
                """);

        Assert.Empty(repo.Docs());
    }

    [Fact]
    public void Criterion_proved_by_manual_verification_passes()
    {
        using var repo = ConcludedSpec("| CA-01 | verificação manual: o dono jogou e confirmou | ok |");

        Assert.Empty(repo.Docs());
    }

    [Fact]
    public void Status_pointing_to_a_missing_spec_is_reported()
    {
        using var repo = new TempRepository().Write("docs/STATUS.md", "---\nfase: fatia\nspec-atual: 9999\n---\n");

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("no specs/9999-* folder", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_status_file_is_reported()
    {
        using var repo = new TempRepository();

        var finding = Assert.Single(repo.Docs());
        Assert.Equal("docs/STATUS.md", finding.Path);
    }

    [Fact]
    public void Adr_numbering_gap_is_reported()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("docs/adr/0001-primeira.md", Adr("0001"))
            .Write("docs/adr/0003-terceira.md", Adr("0003"))
            .Write("docs/adr/README.md", "[1](0001-primeira.md) [3](0003-terceira.md)\n");

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("expected 0002", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Adr_missing_from_the_index_is_reported()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("docs/adr/0001-primeira.md", Adr("0001"))
            .Write("docs/adr/README.md", "# ADRs\n");

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("does not link to 0001-primeira.md", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Replaced_adr_without_replacement_is_reported()
    {
        using var repo = TempRepository.WithValidDocs()
            .Write("docs/adr/0001-primeira.md", Adr("0001", status: "substituida"))
            .Write("docs/adr/README.md", "[1](0001-primeira.md)\n");

        Assert.Contains(repo.Docs(), finding => finding.Message.Contains("substituida-por", StringComparison.Ordinal));
    }

    [Fact]
    public void Tech_debt_item_without_destination_is_reported()
    {
        using var repo = TempRepository.WithValidDocs().Write("docs/TECH_DEBT.md", """
            | ID | Item | Motivo | Destino | Origem |
            |---|---|---|---|---|
            | D-01 | TCP sem criptografia | simplificação | fatia 3 | 0000 |
            | D-02 | Sem limite de conexões | simplificação | depois | 0000 |
            """);

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("found 'depois'", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Proposal_without_status_is_reported()
    {
        using var repo = TempRepository.WithValidDocs().Write("docs/PROPOSALS.md", """
            # Propostas

            ## Abertas

            ### P-0001: com status
            - Status: aberta

            ### P-0002: sem status
            - Tipo: regra
            """);

        var finding = Assert.Single(repo.Docs());
        Assert.Contains("P-0002", finding.Message, StringComparison.Ordinal);
    }

    private static TempRepository ConcludedSpec(string reportRow) => TempRepository.WithValidDocs()
        .Write("specs/9999-teste/spec.md", Spec("concluida", "- CA-01: o bot anda"))
        .Write("specs/9999-teste/plan.md", Plan)
        .Write("specs/9999-teste/tasks.md", "# Tarefas\n")
        .Write("specs/9999-teste/report.md", "# Report\n\n| CA | Prova | Resultado |\n|---|---|---|\n" + reportRow + "\n");

    private static string Spec(string status, string criteria) =>
        $"---\nid: 9999\ntitulo: Teste\nstatus: {status}\n---\n\n# Spec\n\n## Critérios de aceitação\n\n{criteria}\n";

    private static string Adr(string id, string status = "aceita") =>
        $"---\nid: {id}\ntitulo: Decisão\nstatus: {status}\nsubstitui:\nsubstituida-por:\n---\n\n# ADR\n";
}
