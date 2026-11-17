import type { MarketingPageId } from './marketing-match';

export interface MarketingSection {
  readonly heading: string;
  readonly paragraphs: readonly string[];
}

export interface PricingPlan {
  readonly name: string;
  readonly summary: string;
  readonly points: readonly string[];
}

export interface MarketingPage {
  readonly title: string;
  readonly lede: string;
  readonly sections: readonly MarketingSection[];
  readonly plans?: readonly PricingPlan[];
  readonly email?: string;
}

export interface SnapshotSlice {
  readonly name: string;
  readonly y: number;
}

export interface QuestionCountRow {
  readonly type: string;
  readonly count: number;
}

export interface GrowthRow {
  readonly year: number;
  readonly organizationsAdded: number;
  readonly usersAdded: number;
  readonly period: string;
}

export interface SampleCompany {
  readonly name: string;
  readonly sector: string;
  readonly note: string;
}

/** Sample grids on the public home page. The pie charts load live totals instead. */
export const PLATFORM_SNAPSHOT = {
  questions: [
    { type: 'Multiple choice', count: 18420 },
    { type: 'True / false', count: 6320 },
    { type: 'Multiple selection', count: 4185 },
  ] satisfies readonly QuestionCountRow[],
  growth: [
    { year: 2022, organizationsAdded: 4, usersAdded: 320, period: 'Full year' },
    { year: 2023, organizationsAdded: 9, usersAdded: 1450, period: 'Full year' },
    { year: 2024, organizationsAdded: 14, usersAdded: 3860, period: 'Full year' },
    { year: 2025, organizationsAdded: 18, usersAdded: 6120, period: 'Full year' },
    { year: 2026, organizationsAdded: 10, usersAdded: 2640, period: 'Year to date' },
  ] satisfies readonly GrowthRow[],
};

export const SAMPLE_COMPANIES: readonly SampleCompany[] = [
  {
    name: 'Northwind Logistics',
    sector: 'Logistics',
    note: 'Assigns safety and route courses to drivers and warehouse staff.',
  },
  {
    name: 'Contoso Health',
    sector: 'Healthcare',
    note: 'Tracks clinical onboarding and annual refresher training.',
  },
  {
    name: 'Fabrikam Energy',
    sector: 'Energy',
    note: 'Designs field-procedure trainings for site crews.',
  },
  {
    name: 'Harborline Aviation',
    sector: 'Aviation',
    note: 'Keeps crew qualifications and exam results in one place.',
  },
  {
    name: 'Lumenfield University',
    sector: 'Education',
    note: 'Runs staff development separately for each faculty.',
  },
  {
    name: 'Alpine Municipal Academy',
    sector: 'Public sector',
    note: 'Onboards new civic employees with assigned courses.',
  },
];

export const TECHNICAL_STACK: readonly string[] = [
  'Angular',
  'TypeScript',
  '.NET',
  'Entity Framework Core',
  'SQL Server',
  'Keycloak',
  'AG Grid',
  'Highcharts',
];

export const MARKETING_PAGES: Record<MarketingPageId, MarketingPage> = {
  product: {
    title: 'Product',
    lede: 'LearningEdge is a training system for many organizations on one platform. Each organization keeps its own people, courses, and results.',
    sections: [
      {
        heading: 'Organizations',
        paragraphs: [
          'A system admin creates organizations and appoints an organization admin for each one. The organization admin looks after that tenant: staff, trainees, and the training catalog.',
        ],
      },
      {
        heading: 'Courses and questions',
        paragraphs: [
          'Instructors group trainings into categories, upload Word or PDF material, and build question banks and exams. Question types are multiple choice, true / false, and multiple selection.',
        ],
      },
      {
        heading: 'Learners',
        paragraphs: [
          'Learners sign in, open the training assigned to them, and review their scores. A new registration stays pending until an administrator approves it.',
        ],
      },
    ],
  },
  demo: {
    title: 'Demo',
    lede: 'The demo is the application itself. Sign in through Keycloak and you return to the workspace that matches your role.',
    sections: [
      {
        heading: 'What to try',
        paragraphs: [
          'Sign up as a learner and choose an organization, or sign in with an account you already have.',
          'A system admin lands on the platform overview, where organizations and organization admins are created and enabled.',
          'An organization admin manages users and staff. An instructor designs categories, trainings, and exams. A learner opens the training portal.',
        ],
      },
      {
        heading: 'What you will see',
        paragraphs: [
          'Lists show loading, empty, and error states. Tenant users only see their own organization. Switching organization as a system admin clears the previous tenant from the screen.',
        ],
      },
    ],
  },
  pricing: {
    title: 'Pricing',
    lede: 'Choose the scope that matches how many organizations you need to run. Talk to us before a rollout. There is no checkout on this site.',
    sections: [
      {
        heading: 'How plans differ',
        paragraphs: [
          'Every plan includes sign-in, organization isolation, and the learner portal. Broader plans add catalog tools and platform administration.',
        ],
      },
    ],
    plans: [
      {
        name: 'Team',
        summary: 'One organization.',
        points: ['Organization admin, instructors, and learners', 'Categories and assigned training', 'Learner scores'],
      },
      {
        name: 'Organization',
        summary: 'A full training catalog.',
        points: ['Question banks and exams', 'Word and PDF material processing', 'Staff operations and messaging'],
      },
      {
        name: 'Platform',
        summary: 'Many organizations.',
        points: ['System admin for every tenant', 'Enable or disable organizations and admins', 'Separate data for each organization'],
      },
    ],
    email: 'hello@learningedge.example',
  },
  about: {
    title: 'About',
    lede: 'LearningEdge exists so an organization can teach its own people without mixing their records with another tenant.',
    sections: [
      {
        heading: 'Who it is for',
        paragraphs: [
          'Companies, schools, and public teams that need instructors, a course catalog, and a record of who completed the work.',
          'A system admin looks after the platform. Organization admins, instructors, and learners stay inside one organization.',
        ],
      },
      {
        heading: 'How sign-in works',
        paragraphs: [
          'Sign-in is handled by Keycloak. After a successful login the application reads the person from LearningEdge and opens the matching home.',
        ],
      },
    ],
  },
  contact: {
    title: 'Contact',
    lede: 'Write to the LearningEdge team about a rollout, a demo account, or a question the FAQ does not answer.',
    email: 'hello@learningedge.example',
    sections: [
      {
        heading: 'What to include',
        paragraphs: [
          'Your name, organization, and whether you need a system admin, an organization admin, or a learner account.',
          'This page is a contact note. It does not submit a form to the server.',
        ],
      },
    ],
  },
  faq: {
    title: 'FAQ',
    lede: 'Short answers to the questions people ask before they sign in.',
    sections: [
      {
        heading: 'Who can sign in?',
        paragraphs: [
          'Anyone with a LearningEdge account in Keycloak. A system admin can also sign in from a platform role when no separate user row exists.',
        ],
      },
      {
        heading: 'Can I see another organization?',
        paragraphs: [
          'Tenant users stay inside their own organization. A system admin can open any organization to manage it and its administrators.',
        ],
      },
      {
        heading: 'How does a learner join?',
        paragraphs: [
          'They register, choose an organization, and wait for approval. An administrator can also create the account directly.',
        ],
      },
      {
        heading: 'Which files can a training use?',
        paragraphs: ['Instructors upload Word documents and PDFs. The processing screen follows the job from upload through question generation.'],
      },
    ],
  },
  privacy: {
    title: 'Privacy Policy',
    lede: 'This policy describes what the LearningEdge application stores so it can run training for an organization.',
    sections: [
      {
        heading: 'Account data',
        paragraphs: [
          'LearningEdge stores a name, email address, role, organization, and whether the account is pending, active, or inactive. Passwords are handled by Keycloak.',
        ],
      },
      {
        heading: 'Training data',
        paragraphs: [
          'The application stores categories, trainings, uploaded material, questions, exams, assignments, and scores for the organization that created them.',
        ],
      },
      {
        heading: 'How it is used',
        paragraphs: [
          'The data is used to sign people in, show the right workspace, and keep one organization from reading another. It is not sold.',
        ],
      },
    ],
  },
  copyright: {
    title: 'Copyright',
    lede: 'The LearningEdge application and this website are protected. Training content uploaded by an organization stays with that organization.',
    sections: [
      {
        heading: 'The application',
        paragraphs: [
          'The LearningEdge name, interface, and software may be used by the organizations that run on this platform. Copying the application for another product requires permission.',
        ],
      },
      {
        heading: 'Your content',
        paragraphs: [
          'Documents, questions, and exam text created inside an organization belong to that organization. LearningEdge stores them so the organization’s instructors and learners can use them.',
        ],
      },
    ],
  },
};
