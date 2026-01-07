import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { RefreshCw, UserPlus } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Table, Column } from '@/components/ui/Table'
import { Badge } from '@/components/ui/Badge'
import { Modal } from '@/components/ui/Modal'
import { Select } from '@/components/ui/Select'
import { PageLoading } from '@/components/ui/Loading'
import { Alert } from '@/components/ui/Alert'
import { scheduleService, vehicleService, driverService } from '@/services'
import { formatDate } from '@/lib/utils'
import type { Schedule, Vehicle, Driver } from '@/types'

const assignSchema = z.object({
  vehicleId: z.coerce.number().min(1, 'Pilih kendaraan'),
  driverId: z.coerce.number().min(1, 'Pilih driver')
})

type AssignFormData = z.infer<typeof assignSchema>

export default function AdminSchedulePage() {
  const queryClient = useQueryClient()
  const [selectedSchedule, setSelectedSchedule] = useState<Schedule | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<AssignFormData>({
    resolver: zodResolver(assignSchema)
  })

  const { data: schedules, isLoading } = useQuery({
    queryKey: ['all-schedules'],
    queryFn: async () => {
      const response = await scheduleService.getAll()
      return response.data || []
    }
  })

  const { data: waitingResources } = useQuery({
    queryKey: ['waiting-resources'],
    queryFn: async () => {
      const response = await scheduleService.getWaitingResources()
      return response.data || []
    }
  })

  const { data: availableVehicles } = useQuery({
    queryKey: ['available-vehicles'],
    queryFn: async () => {
      const response = await vehicleService.getAvailable()
      return response.data || []
    },
    enabled: selectedSchedule !== null
  })

  const { data: availableDrivers } = useQuery({
    queryKey: ['available-drivers'],
    queryFn: async () => {
      const response = await driverService.getAvailable()
      return response.data || []
    },
    enabled: selectedSchedule !== null
  })

  const assignMutation = useMutation({
    mutationFn: async (data: AssignFormData) => {
      if (!selectedSchedule) return
      return scheduleService.assign({
        loanRequestId: selectedSchedule.loanRequestId,
        vehicleId: data.vehicleId,
        driverId: data.driverId
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['all-schedules'] })
      queryClient.invalidateQueries({ queryKey: ['waiting-resources'] })
      closeModal()
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } }
      setError(error.response?.data?.message || 'Gagal menetapkan jadwal')
    }
  })

  const retryMutation = useMutation({
    mutationFn: (loanRequestId: number) => scheduleService.retryScheduling(loanRequestId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['all-schedules'] })
      queryClient.invalidateQueries({ queryKey: ['waiting-resources'] })
    }
  })

  const openAssignModal = (schedule: Schedule) => {
    setSelectedSchedule(schedule)
    setError(null)
    reset()
  }

  const closeModal = () => {
    setSelectedSchedule(null)
    setError(null)
    reset()
  }

  const onSubmit = (data: AssignFormData) => {
    assignMutation.mutate(data)
  }

  const columns: Column<Schedule>[] = [
    {
      key: 'id',
      header: 'ID',
      render: (item) => <span className="font-mono text-xs">#{item.id}</span>
    },
    {
      key: 'purpose',
      header: 'Tujuan',
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.purpose || 'N/A'}</p>
          <p className="text-sm text-gray-500">{item.destination}</p>
        </div>
      )
    },
    {
      key: 'departureDate',
      header: 'Tanggal',
      render: (item) => (
        <div>
          <p className="text-sm">{formatDate(item.departureDate)}</p>
          <p className="text-xs text-gray-500">
            {item.departureTime} - {item.returnTime}
          </p>
        </div>
      )
    },
    {
      key: 'vehiclePlateNumber',
      header: 'Kendaraan',
      render: (item) => item.vehiclePlateNumber ? (
        <div>
          <p className="font-medium text-gray-900">{item.vehiclePlateNumber}</p>
          <p className="text-sm text-gray-500">{item.vehicleModel}</p>
        </div>
      ) : (
        <span className="text-gray-400 text-sm">Belum ditentukan</span>
      )
    },
    {
      key: 'driverName',
      header: 'Driver',
      render: (item) => item.driverName ? (
        <span className="font-medium">{item.driverName}</span>
      ) : (
        <span className="text-gray-400 text-sm">Belum ditentukan</span>
      )
    },
    {
      key: 'status',
      header: 'Status',
      render: (item) => <Badge status={item.status} />
    },
    {
      key: 'actions',
      header: 'Aksi',
      render: (item) => (
        <div className="flex items-center gap-2">
          {item.status === 'WAITING_RESOURCE' && (
            <>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => openAssignModal(item)}
                leftIcon={<UserPlus className="w-4 h-4" />}
              >
                Assign
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => retryMutation.mutate(item.loanRequestId)}
                isLoading={retryMutation.isPending}
              >
                <RefreshCw className="w-4 h-4" />
              </Button>
            </>
          )}
        </div>
      )
    }
  ]

  if (isLoading) return <PageLoading />

  const stats = {
    total: schedules?.length || 0,
    scheduled: schedules?.filter(s => s.status === 'SCHEDULED').length || 0,
    inProgress: schedules?.filter(s => s.status === 'IN_PROGRESS').length || 0,
    completed: schedules?.filter(s => s.status === 'COMPLETED').length || 0,
    waiting: waitingResources?.length || 0
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Jadwal Kendaraan</h1>
        <p className="text-gray-600">Kelola jadwal peminjaman kendaraan</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Total</p>
            <p className="text-2xl font-bold text-gray-900">{stats.total}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Terjadwal</p>
            <p className="text-2xl font-bold text-blue-600">{stats.scheduled}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Dalam Perjalanan</p>
            <p className="text-2xl font-bold text-purple-600">{stats.inProgress}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Selesai</p>
            <p className="text-2xl font-bold text-green-600">{stats.completed}</p>
          </CardContent>
        </Card>
        <Card className={stats.waiting > 0 ? 'bg-amber-50 border-amber-200' : ''}>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Menunggu Resource</p>
            <p className="text-2xl font-bold text-amber-600">{stats.waiting}</p>
          </CardContent>
        </Card>
      </div>

      {/* Waiting Resources Alert */}
      {stats.waiting > 0 && (
        <Alert variant="warning" title="Perhatian">
          Terdapat {stats.waiting} jadwal yang menunggu kendaraan atau driver tersedia.
          Silakan assign secara manual atau tunggu hingga resource tersedia.
        </Alert>
      )}

      {/* Table */}
      <Card>
        <CardHeader>
          <CardTitle>Daftar Jadwal</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table
            columns={columns}
            data={schedules || []}
            keyExtractor={(item) => item.id}
            emptyMessage="Belum ada jadwal"
          />
        </CardContent>
      </Card>

      {/* Assign Modal */}
      <Modal
        isOpen={selectedSchedule !== null}
        onClose={closeModal}
        title="Tetapkan Kendaraan & Driver"
        description={`Jadwal untuk ${selectedSchedule?.destination}`}
      >
        {error && (
          <Alert variant="error" className="mb-4" onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="p-4 bg-gray-50 rounded-lg mb-4">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500">Tanggal</p>
                <p className="font-medium">
                  {selectedSchedule && formatDate(selectedSchedule.departureDate)}
                </p>
              </div>
              <div>
                <p className="text-gray-500">Waktu</p>
                <p className="font-medium">
                  {selectedSchedule?.departureTime} - {selectedSchedule?.returnTime}
                </p>
              </div>
            </div>
          </div>

          <Select
            label="Kendaraan *"
            placeholder="Pilih kendaraan"
            options={(availableVehicles || []).map((v: Vehicle) => ({
              value: v.id,
              label: `${v.plateNumber} - ${v.model} (${v.capacity} seat)`
            }))}
            error={errors.vehicleId?.message}
            {...register('vehicleId')}
          />

          <Select
            label="Driver *"
            placeholder="Pilih driver"
            options={(availableDrivers || []).map((d: Driver) => ({
              value: d.id,
              label: `${d.name} - ${d.phoneNumber}`
            }))}
            error={errors.driverId?.message}
            {...register('driverId')}
          />

          <div className="flex justify-end gap-3 mt-6">
            <Button variant="ghost" type="button" onClick={closeModal}>
              Batal
            </Button>
            <Button
              type="submit"
              isLoading={assignMutation.isPending}
            >
              Tetapkan
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
