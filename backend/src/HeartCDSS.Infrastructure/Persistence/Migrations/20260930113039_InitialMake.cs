using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HeartCDSS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ml_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    calibration_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    decision_threshold = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    target_sensitivity = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    brier_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    roc_auc = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    pr_auc = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true),
                    artifact_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deployed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    made_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ml_models", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    mrn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    biological_sex = table.Column<short>(type: "smallint", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    emergency_contact_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    made_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prediction_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    risk_assessment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_payload = table.Column<string>(type: "jsonb", nullable: false),
                    response_payload = table.Column<string>(type: "jsonb", nullable: true),
                    explaining_features = table.Column<string>(type: "jsonb", nullable: true),
                    execution_time_ms = table.Column<int>(type: "integer", nullable: false),
                    inference_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    client_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    made_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prediction_audits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    role_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    made_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    medical_license_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    made_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinical_encounters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clinician_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    encounter_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    chief_complaint = table.Column<string>(type: "text", nullable: true),
                    encounter_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    made_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_encounters", x => x.id);
                    table.ForeignKey(
                        name: "FK_clinical_encounters_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clinical_indicators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    age_at_encounter = table.Column<short>(type: "smallint", nullable: false),
                    sex = table.Column<short>(type: "smallint", nullable: false),
                    chest_pain_type = table.Column<short>(type: "smallint", nullable: false),
                    resting_bp = table.Column<short>(type: "smallint", nullable: false),
                    diastolic_bp = table.Column<short>(type: "smallint", nullable: true),
                    serum_cholesterol = table.Column<short>(type: "smallint", nullable: false),
                    fasting_blood_sugar = table.Column<short>(type: "smallint", nullable: false),
                    resting_ecg = table.Column<short>(type: "smallint", nullable: false),
                    max_heart_rate = table.Column<short>(type: "smallint", nullable: false),
                    exercise_angina = table.Column<short>(type: "smallint", nullable: false),
                    oldpeak_st_depression = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false),
                    st_slope = table.Column<short>(type: "smallint", nullable: false),
                    fluoroscopy_vessels = table.Column<short>(type: "smallint", nullable: false),
                    thalassemia = table.Column<short>(type: "smallint", nullable: false),
                    made_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_indicators", x => x.id);
                    table.ForeignKey(
                        name: "FK_clinical_indicators_clinical_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalTable: "clinical_encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    risk_probability = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    risk_band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ci_lower_bound = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ci_upper_bound = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ci_confidence_level = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    ci_margin_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    decision_threshold = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    base_expected_value = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    cdss_advisory_text = table.Column<string>(type: "text", nullable: false),
                    recommended_action = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    assessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    MadeAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_risk_assessments_clinical_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalTable: "clinical_encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shap_attributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    risk_assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    feature_display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    feature_value_display = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attribution_value = table.Column<decimal>(type: "numeric(7,5)", precision: 7, scale: 5, nullable: false),
                    impact_direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    percentage_impact = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    rank_order = table.Column<short>(type: "smallint", nullable: false),
                    MadeAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shap_attributions", x => x.id);
                    table.ForeignKey(
                        name: "FK_shap_attributions_risk_assessments_risk_assessment_id",
                        column: x => x.risk_assessment_id,
                        principalTable: "risk_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "deleted_at", "description", "made_at", "role_name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, "Cardiologists and attending physicians with clinical order and assessment authority", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(511), new TimeSpan(0, 0, 0, 0, 0)), "Physician", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(515), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, "Nurses and medical assistants authorized to intake vitals and view patient rosters", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(1299), new TimeSpan(0, 0, 0, 0, 0)), "ClinicalStaff", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(1301), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), null, "System administrators managing user accounts and model registries", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(1303), new TimeSpan(0, 0, 0, 0, 0)), "Admin", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(1303), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), null, "Compliance auditors with read-only access to prediction logs and audit trails", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(1304), new TimeSpan(0, 0, 0, 0, 0)), "Auditor", new DateTimeOffset(new DateTime(2026, 9, 30, 11, 30, 37, 789, DateTimeKind.Unspecified).AddTicks(1305), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "idx_encounters_clinician",
                table: "clinical_encounters",
                column: "clinician_id");

            migrationBuilder.CreateIndex(
                name: "idx_encounters_number",
                table: "clinical_encounters",
                column: "encounter_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_encounters_patient_date",
                table: "clinical_encounters",
                columns: new[] { "patient_id", "encounter_date" });

            migrationBuilder.CreateIndex(
                name: "idx_encounters_status",
                table: "clinical_encounters",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_clinical_indicators_encounter",
                table: "clinical_indicators",
                column: "encounter_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ml_models_active",
                table: "ml_models",
                column: "is_active",
                filter: "is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_ml_models_version",
                table: "ml_models",
                column: "model_version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_patients_dob",
                table: "patients",
                column: "date_of_birth");

            migrationBuilder.CreateIndex(
                name: "idx_patients_mrn",
                table: "patients",
                column: "mrn",
                unique: true,
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_patients_name",
                table: "patients",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "idx_prediction_audits_correlation",
                table: "prediction_audits",
                column: "correlation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_prediction_audits_encounter",
                table: "prediction_audits",
                column: "encounter_id");

            migrationBuilder.CreateIndex(
                name: "idx_prediction_audits_explaining_json",
                table: "prediction_audits",
                column: "explaining_features")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_prediction_audits_made",
                table: "prediction_audits",
                column: "made_at");

            migrationBuilder.CreateIndex(
                name: "idx_prediction_audits_req_json",
                table: "prediction_audits",
                column: "request_payload")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_prediction_audits_res_json",
                table: "prediction_audits",
                column: "response_payload")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_prediction_audits_user",
                table: "prediction_audits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_risk_assessments_band",
                table: "risk_assessments",
                column: "risk_band");

            migrationBuilder.CreateIndex(
                name: "idx_risk_assessments_encounter",
                table: "risk_assessments",
                columns: new[] { "encounter_id", "assessed_at" });

            migrationBuilder.CreateIndex(
                name: "idx_risk_assessments_model",
                table: "risk_assessments",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "idx_roles_name",
                table: "roles",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_shap_attributions_assessment",
                table: "shap_attributions",
                columns: new[] { "risk_assessment_id", "rank_order" });

            migrationBuilder.CreateIndex(
                name: "idx_shap_attributions_direction",
                table: "shap_attributions",
                columns: new[] { "risk_assessment_id", "impact_direction" });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_users_username",
                table: "users",
                column: "username",
                unique: true,
                filter: "is_deleted = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinical_indicators");

            migrationBuilder.DropTable(
                name: "ml_models");

            migrationBuilder.DropTable(
                name: "prediction_audits");

            migrationBuilder.DropTable(
                name: "shap_attributions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "risk_assessments");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "clinical_encounters");

            migrationBuilder.DropTable(
                name: "patients");
        }
    }
}
