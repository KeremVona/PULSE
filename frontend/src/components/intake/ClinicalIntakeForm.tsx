import React, { useEffect, useMemo } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import {
  makeEncounterAsync,
  updateIndicatorsBatch,
  resetEncounterForm
} from '@/store/slices/encounterSlice';
import {
  makeEncounterFormSchema,
  makePhysiologicalWarnings,
  EncounterFormData,
  PhysiologicalWarning
} from '@/validation/encounterSchema';
import { FormField } from '@/components/intake/FormField';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/common/Card';
import { Button } from '@/components/common/Button';
import { Badge } from '@/components/common/Badge';
import {
  EncounterType,
  ChestPainType,
  RestingEcgType,
  SlopeType,
  ThalType,
  ClinicalIndicatorsInput
} from '@/types/clinical';
import {
  Activity,
  HeartPulse,
  RotateCcw,
  Sparkles,
  AlertTriangle,
  Stethoscope,
  FileText
} from 'lucide-react';
import { makeClassName } from '@/utils/cn';

interface ClinicalIntakeFormProps {
  onSuccessNavigate?: () => void;
  className?: string;
}

export const ClinicalIntakeForm: React.FC<ClinicalIntakeFormProps> = ({
  onSuccessNavigate,
  className
}) => {
  const dispatch = useAppDispatch();
  const encounterState = useAppSelector((state) => state.encounter);

  const madePatientId = encounterState.activePatientId;
  const madeFormSchema = useMemo(() => makeEncounterFormSchema(), []);

  const {
    control,
    handleSubmit,
    watch,
    reset,
    setValue,
    formState: { errors, isDirty }
  } = useForm<EncounterFormData>({
    resolver: zodResolver(madeFormSchema),
    defaultValues: {
      encounterNumber: encounterState.encounterNumber,
      encounterType: encounterState.encounterType,
      chiefComplaint: encounterState.chiefComplaint,
      indicators: encounterState.indicators
    },
    mode: 'onChange'
  });

  // Synchronize when a historical encounter is selected in Redux
  const selectedEncounter = encounterState.selectedEncounter;
  useEffect(() => {
    if (selectedEncounter) {
      reset({
        encounterNumber: encounterState.encounterNumber,
        encounterType: selectedEncounter.encounterType,
        chiefComplaint: encounterState.chiefComplaint,
        indicators: selectedEncounter.indicators || encounterState.indicators
      });
    }
  }, [selectedEncounter, reset]);

  // Watch current indicators to evaluate real-time physiological warnings
  const watchedIndicators = watch('indicators');

  // Evaluate clinical safety warnings dynamically
  const madeWarnings: PhysiologicalWarning[] = useMemo(() => {
    return makePhysiologicalWarnings(watchedIndicators || {});
  }, [watchedIndicators]);

  // Lookup map for field-specific warnings
  const madeWarningMap = useMemo(() => {
    const map = new Map<string, PhysiologicalWarning>();
    for (const w of madeWarnings) {
      map.set(w.field, w);
    }
    return map;
  }, [madeWarnings]);

  const makeSubmitHandler = async (data: EncounterFormData) => {
    const madeRequestData = {
      encounterNumber: data.encounterNumber,
      encounterType: data.encounterType,
      chiefComplaint: data.chiefComplaint || null,
      indicators: data.indicators
    };

    const madeAction = await dispatch(
      makeEncounterAsync({
        patientId: madePatientId,
        requestData: madeRequestData
      })
    );

    if (makeEncounterAsync.fulfilled.match(madeAction)) {
      if (onSuccessNavigate) {
        onSuccessNavigate();
      }
    }
  };

  const makeHighRiskPreset = () => {
    const highRiskIndicators: ClinicalIndicatorsInput = {
      age: 63,
      sex: 1,
      chestPainType: ChestPainType.TypicalAngina,
      restingBp: 165,
      diastolicBp: 98,
      serumCholesterol: 288,
      fastingBloodSugar: 1,
      restingEcg: RestingEcgType.LeftVentricularHypertrophy,
      maxHeartRate: 135,
      exerciseAngina: 1,
      oldpeakStDepression: 3.1,
      slope: SlopeType.Downsloping,
      fluoroscopyVessels: 2,
      thalassemia: ThalType.ReversibleDefect
    };

    setValue('indicators', highRiskIndicators, { shouldValidate: true, shouldDirty: true });
    setValue('encounterType', EncounterType.EmergencyTriage, { shouldValidate: true, shouldDirty: true });
    setValue(
      'chiefComplaint',
      'Acute substernal chest pressure with radiation to left arm during physical exertion.',
      { shouldValidate: true, shouldDirty: true }
    );
    dispatch(updateIndicatorsBatch(highRiskIndicators));
  };

  const makeLowRiskPreset = () => {
    const lowRiskIndicators: ClinicalIndicatorsInput = {
      age: 38,
      sex: 0,
      chestPainType: ChestPainType.NonAnginal,
      restingBp: 115,
      diastolicBp: 75,
      serumCholesterol: 180,
      fastingBloodSugar: 0,
      restingEcg: RestingEcgType.Normal,
      maxHeartRate: 172,
      exerciseAngina: 0,
      oldpeakStDepression: 0.2,
      slope: SlopeType.Upsloping,
      fluoroscopyVessels: 0,
      thalassemia: ThalType.Normal
    };

    setValue('indicators', lowRiskIndicators, { shouldValidate: true, shouldDirty: true });
    setValue('encounterType', EncounterType.OutpatientScreening, { shouldValidate: true, shouldDirty: true });
    setValue(
      'chiefComplaint',
      'Annual executive cardiovascular wellness screening; patient is asymptomatic.',
      { shouldValidate: true, shouldDirty: true }
    );
    dispatch(updateIndicatorsBatch(lowRiskIndicators));
  };

  const makeResetHandler = () => {
    dispatch(resetEncounterForm());
    const madeDefaults = {
      age: 55,
      sex: 1,
      chestPainType: ChestPainType.AtypicalAngina,
      restingBp: 130,
      diastolicBp: 80,
      serumCholesterol: 210,
      fastingBloodSugar: 0,
      restingEcg: RestingEcgType.Normal,
      maxHeartRate: 150,
      exerciseAngina: 0,
      oldpeakStDepression: 1.0,
      slope: SlopeType.Flat,
      fluoroscopyVessels: 0,
      thalassemia: ThalType.Normal
    };
    reset({
      encounterNumber: encounterState.encounterNumber,
      encounterType: EncounterType.OutpatientScreening,
      chiefComplaint: 'Routine cardiovascular evaluation with risk assessment.',
      indicators: madeDefaults
    });
  };

  return (
    <form
      onSubmit={handleSubmit(makeSubmitHandler)}
      className={makeClassName('space-y-6 select-none', className)}
      noValidate
    >
      {/* Top Header & Actions Strip */}
      <div className="flex flex-wrap items-center justify-between gap-4 bg-white p-4 rounded-xl border border-slate-200">
        <div>
          <div className="flex items-center gap-2">
            <Stethoscope className="w-5 h-5 text-sky-700" />
            <h2 className="text-base font-bold text-slate-900">
              Two-Column Clinical Intake & Vitals Entry
            </h2>
            <Badge variant={isDirty || encounterState.isDirty ? 'moderate' : 'neutral'}>
              {isDirty || encounterState.isDirty ? 'Unsaved Modifications' : 'Synced'}
            </Badge>
          </div>
          <p className="text-xs text-slate-500 mt-0.5">
            Strict physiological boundaries matching FluentValidation clinical limits.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            leftIcon={<Sparkles className="w-3.5 h-3.5 text-rose-600" />}
            onClick={makeHighRiskPreset}
          >
            Prefill High-Risk Profile
          </Button>

          <Button
            type="button"
            variant="secondary"
            size="sm"
            leftIcon={<Sparkles className="w-3.5 h-3.5 text-emerald-600" />}
            onClick={makeLowRiskPreset}
          >
            Prefill Low-Risk Profile
          </Button>

          <Button
            type="button"
            variant="ghost"
            size="sm"
            leftIcon={<RotateCcw className="w-3.5 h-3.5" />}
            onClick={makeResetHandler}
          >
            Reset Baseline
          </Button>

          <Button
            type="submit"
            variant="primary"
            size="md"
            leftIcon={<Activity className="w-4 h-4" />}
            isLoading={encounterState.isSubmitting}
          >
            Execute ML Risk Evaluation
          </Button>
        </div>
      </div>

      {/* Real-time Physiological Warning Callout */}
      {madeWarnings.length > 0 && (
        <div className="p-4 rounded-xl bg-amber-50/90 border border-amber-300 shadow-2xs space-y-2">
          <div className="flex items-center gap-2 text-amber-900 font-semibold text-sm">
            <AlertTriangle className="w-4 h-4 text-amber-700" />
            <span>Active Physiological Pre-Inference Alerts ({madeWarnings.length})</span>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-xs text-amber-800">
            {madeWarnings.map((w, idx) => (
              <div
                key={idx}
                className={makeClassName(
                  'p-2.5 rounded-lg border bg-white/70 flex items-start gap-2',
                  w.severity === 'critical' ? 'border-rose-300 text-rose-900' : 'border-amber-200'
                )}
              >
                <span
                  className={makeClassName(
                    'w-2 h-2 rounded-full shrink-0 mt-1',
                    w.severity === 'critical' ? 'bg-rose-500' : 'bg-amber-500'
                  )}
                />
                <div>
                  <strong className="font-semibold block">{w.title}</strong>
                  <span className="text-[11px] leading-snug">{w.message}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Two-Column Responsive Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* COLUMN 1: Cardiovascular & Hemodynamic Indicators */}
        <Card className="border-t-4 border-t-sky-700">
          <CardHeader>
            <div className="flex items-center gap-2">
              <HeartPulse className="w-5 h-5 text-sky-700" />
              <div>
                <CardTitle>Column 1: Hemodynamics & Exercise Vitals</CardTitle>
                <CardDescription>Blood pressure, lipid panel, and electrocardiography</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {/* Resting Systolic BP */}
              <FormField
                label="Resting Systolic Blood Pressure"
                name="indicators.restingBp"
                unit="mmHg"
                hint="Bounds: 60 - 260 mmHg"
                required
                error={errors.indicators?.restingBp?.message}
                warning={madeWarningMap.get('restingBp')}
              >
                <Controller
                  name="indicators.restingBp"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      id="indicators.restingBp"
                      type="number"
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value === '' ? '' : Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                    />
                  )}
                />
              </FormField>

              {/* Resting Diastolic BP */}
              <FormField
                label="Resting Diastolic Blood Pressure"
                name="indicators.diastolicBp"
                unit="mmHg"
                hint="Bounds: 40 - 140 mmHg"
                required
                error={errors.indicators?.diastolicBp?.message}
                warning={madeWarningMap.get('diastolicBp')}
              >
                <Controller
                  name="indicators.diastolicBp"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      id="indicators.diastolicBp"
                      type="number"
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value === '' ? '' : Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                    />
                  )}
                />
              </FormField>

              {/* Serum Cholesterol */}
              <FormField
                label="Total Serum Cholesterol"
                name="indicators.serumCholesterol"
                unit="mg/dL"
                hint="Bounds: 80 - 600 mg/dL"
                required
                error={errors.indicators?.serumCholesterol?.message}
                warning={madeWarningMap.get('serumCholesterol')}
              >
                <Controller
                  name="indicators.serumCholesterol"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      id="indicators.serumCholesterol"
                      type="number"
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value === '' ? '' : Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                    />
                  )}
                />
              </FormField>

              {/* Fasting Blood Sugar Flag */}
              <FormField
                label="Fasting Blood Sugar > 120 mg/dL"
                name="indicators.fastingBloodSugar"
                hint="Glycemic state threshold"
                required
                error={errors.indicators?.fastingBloodSugar?.message}
              >
                <Controller
                  name="indicators.fastingBloodSugar"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="indicators.fastingBloodSugar"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={0}>Normal (&lt;= 120 mg/dL)</option>
                      <option value={1}>Elevated (&gt; 120 mg/dL)</option>
                    </select>
                  )}
                />
              </FormField>

              {/* Max Heart Rate Achieved */}
              <FormField
                label="Max Heart Rate Achieved"
                name="indicators.maxHeartRate"
                unit="bpm"
                hint="Bounds: 40 - 220 bpm"
                required
                error={errors.indicators?.maxHeartRate?.message}
              >
                <Controller
                  name="indicators.maxHeartRate"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      id="indicators.maxHeartRate"
                      type="number"
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value === '' ? '' : Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                    />
                  )}
                />
              </FormField>

              {/* Exercise Induced Angina */}
              <FormField
                label="Exercise-Induced Angina (exang)"
                name="indicators.exerciseAngina"
                hint="Angina provoked by treadmill"
                required
                error={errors.indicators?.exerciseAngina?.message}
                warning={madeWarningMap.get('exerciseAngina')}
              >
                <Controller
                  name="indicators.exerciseAngina"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="indicators.exerciseAngina"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={0}>No (Absent)</option>
                      <option value={1}>Yes (Provoked)</option>
                    </select>
                  )}
                />
              </FormField>
            </div>

            {/* ST Segment Depression */}
            <FormField
              label="ST Depression Induced by Exercise (oldpeak)"
              name="indicators.oldpeakStDepression"
              unit="mm"
              hint="Continuous measurement relative to baseline rest (0.0 - 6.5 mm)"
              required
              error={errors.indicators?.oldpeakStDepression?.message}
              warning={madeWarningMap.get('oldpeakStDepression')}
            >
              <Controller
                name="indicators.oldpeakStDepression"
                control={control}
                render={({ field }) => (
                  <input
                    {...field}
                    id="indicators.oldpeakStDepression"
                    type="number"
                    step="0.1"
                    value={field.value ?? ''}
                    onChange={(e) => field.onChange(e.target.value === '' ? '' : Number(e.target.value))}
                    className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono font-semibold"
                  />
                )}
              />
            </FormField>
          </CardContent>
        </Card>

        {/* COLUMN 2: Demographics, History & Diagnostic Findings */}
        <Card className="border-t-4 border-t-indigo-700">
          <CardHeader>
            <div className="flex items-center gap-2">
              <FileText className="w-5 h-5 text-indigo-700" />
              <div>
                <CardTitle>Column 2: Demographics & Clinical Diagnostic Findings</CardTitle>
                <CardDescription>Chest pain phenotype, ECG morphology, and fluoroscopy</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {/* Patient Age */}
              <FormField
                label="Patient Age"
                name="indicators.age"
                unit="years"
                hint="Bounds: 18 - 110 years"
                required
                error={errors.indicators?.age?.message}
              >
                <Controller
                  name="indicators.age"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      id="indicators.age"
                      type="number"
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value === '' ? '' : Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500"
                    />
                  )}
                />
              </FormField>

              {/* Biological Sex */}
              <FormField
                label="Biological Sex"
                name="indicators.sex"
                hint="Administrative baseline"
                required
                error={errors.indicators?.sex?.message}
              >
                <Controller
                  name="indicators.sex"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="indicators.sex"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={1}>Male (1)</option>
                      <option value={0}>Female (0)</option>
                    </select>
                  )}
                />
              </FormField>

              {/* Chest Pain Classification */}
              <FormField
                label="Chest Pain Type (cp)"
                name="indicators.chestPainType"
                hint="Diamond-Forrester classification"
                required
                error={errors.indicators?.chestPainType?.message}
              >
                <Controller
                  name="indicators.chestPainType"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="indicators.chestPainType"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={ChestPainType.TypicalAngina}>Typical Angina (1)</option>
                      <option value={ChestPainType.AtypicalAngina}>Atypical Angina (2)</option>
                      <option value={ChestPainType.NonAnginal}>Non-Anginal Pain (3)</option>
                      <option value={ChestPainType.Asymptomatic}>Asymptomatic (4)</option>
                    </select>
                  )}
                />
              </FormField>

              {/* Resting ECG */}
              <FormField
                label="Resting Electrocardiogram (restecg)"
                name="indicators.restingEcg"
                hint="Baseline ECG finding"
                required
                error={errors.indicators?.restingEcg?.message}
              >
                <Controller
                  name="indicators.restingEcg"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="indicators.restingEcg"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={RestingEcgType.Normal}>Normal (0)</option>
                      <option value={RestingEcgType.SttAbnormality}>ST-T Wave Abnormality (1)</option>
                      <option value={RestingEcgType.LeftVentricularHypertrophy}>LV Hypertrophy (2)</option>
                    </select>
                  )}
                />
              </FormField>

              {/* Peak Exercise ST Slope */}
              <FormField
                label="Peak Exercise ST Slope (slope)"
                name="indicators.slope"
                hint="ST segment vector at peak load"
                required
                error={errors.indicators?.slope?.message}
              >
                <Controller
                  name="indicators.slope"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="indicators.slope"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={SlopeType.Upsloping}>Upsloping (1)</option>
                      <option value={SlopeType.Flat}>Flat (2)</option>
                      <option value={SlopeType.Downsloping}>Downsloping (3)</option>
                    </select>
                  )}
                />
              </FormField>

              {/* Fluoroscopy Vessels */}
              <FormField
                label="Fluoroscopy Vessels Colored (ca)"
                name="indicators.fluoroscopyVessels"
                hint="Major coronary vessels (0 - 3)"
                required
                error={errors.indicators?.fluoroscopyVessels?.message}
                warning={madeWarningMap.get('fluoroscopyVessels')}
              >
                <Controller
                  name="indicators.fluoroscopyVessels"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="indicators.fluoroscopyVessels"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={0}>0 Vessels</option>
                      <option value={1}>1 Vessel</option>
                      <option value={2}>2 Vessels</option>
                      <option value={3}>3 Vessels</option>
                    </select>
                  )}
                />
              </FormField>
            </div>

            {/* Thalassemia Scintigraphy */}
            <FormField
              label="Thalassemia Defect Scintigraphy (thal)"
              name="indicators.thalassemia"
              hint="Nuclear myocardial perfusion scintigraphy"
              required
              error={errors.indicators?.thalassemia?.message}
            >
              <Controller
                name="indicators.thalassemia"
                control={control}
                render={({ field }) => (
                  <select
                    {...field}
                    id="indicators.thalassemia"
                    value={field.value}
                    onChange={(e) => field.onChange(Number(e.target.value))}
                    className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                  >
                    <option value={ThalType.Normal}>Normal Perfusion (3)</option>
                    <option value={ThalType.FixedDefect}>Fixed Perfusion Defect / Infarction (6)</option>
                    <option value={ThalType.ReversibleDefect}>Reversible Defect / Transient Ischemia (7)</option>
                  </select>
                )}
              />
            </FormField>

            {/* Encounter Metadata: Number & Type */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-slate-100">
              <FormField
                label="Encounter Number"
                name="encounterNumber"
                hint="Identifier"
                required
                error={errors.encounterNumber?.message}
              >
                <Controller
                  name="encounterNumber"
                  control={control}
                  render={({ field }) => (
                    <input
                      {...field}
                      id="encounterNumber"
                      type="text"
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                    />
                  )}
                />
              </FormField>

              <FormField
                label="Clinical Care Setting"
                name="encounterType"
                hint="Administrative context"
                required
                error={errors.encounterType?.message}
              >
                <Controller
                  name="encounterType"
                  control={control}
                  render={({ field }) => (
                    <select
                      {...field}
                      id="encounterType"
                      value={field.value}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500 bg-white"
                    >
                      <option value={EncounterType.OutpatientScreening}>Outpatient Screening</option>
                      <option value={EncounterType.EmergencyTriage}>Emergency Triage</option>
                      <option value={EncounterType.InpatientConsult}>Inpatient Consult</option>
                      <option value={EncounterType.CardiologyFollowUp}>Cardiology Follow-Up</option>
                    </select>
                  )}
                />
              </FormField>
            </div>

            {/* Chief Complaint */}
            <FormField
              label="Chief Complaint & Symptom Presentation"
              name="chiefComplaint"
              hint="Clinical narrative (Max 1000 characters)"
              error={errors.chiefComplaint?.message}
            >
              <Controller
                name="chiefComplaint"
                control={control}
                render={({ field }) => (
                  <textarea
                    {...field}
                    id="chiefComplaint"
                    rows={2}
                    value={field.value ?? ''}
                    placeholder="Enter presenting patient complaints, trigger factors, and onset..."
                    className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                )}
              />
            </FormField>
          </CardContent>
        </Card>
      </div>

      {/* Bottom Sticky Action Toolbar */}
      <div className="flex items-center justify-between p-4 bg-white rounded-xl border border-slate-200 shadow-sm sticky bottom-4 z-30">
        <div className="flex items-center gap-3">
          <span className="text-xs text-slate-500">
            Encounter: <strong className="font-mono text-slate-800">{encounterState.encounterNumber}</strong>
          </span>
          <span className="text-slate-300">|</span>
          <span className="text-xs text-slate-500">
            Active Patient: <strong className="text-slate-800">{encounterState.activePatientName}</strong>
          </span>
        </div>

        <div className="flex items-center gap-2">
          <Button
            type="submit"
            variant="primary"
            size="md"
            leftIcon={<Activity className="w-4 h-4" />}
            isLoading={encounterState.isSubmitting}
          >
            Execute ML Risk Evaluation
          </Button>
        </div>
      </div>
    </form>
  );
};
